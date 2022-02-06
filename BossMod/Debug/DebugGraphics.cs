﻿using Dalamud.Interface;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace BossMod
{
    class DebugGraphics
    {
        private class WatchedRenderObject
        {
            public List<uint> Data = new();
            public List<(int, int)> Modifications = new();
            public bool Live;
        }

        private bool _showGraphicsLeafCharactersOnly = true;
        private Dictionary<IntPtr, WatchedRenderObject> _watchedRenderObjects = new();

        public unsafe void DrawSceneTree()
        {
            ImGui.Checkbox("Show only leafs of Character type", ref _showGraphicsLeafCharactersOnly);
            var root = FindSceneRoot();
            if (root != null)
            {
                DrawSceneNode(root);
            }
        }

        private unsafe void DrawSceneNode(FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Object* o)
        {
            var start = o;
            do
            {
                var nodeText = $"{SceneNodeText(o)}###{(IntPtr)o}";
                ImGuiTreeNodeFlags nodeFlags = (o->ChildObject != null ? ImGuiTreeNodeFlags.None : ImGuiTreeNodeFlags.Leaf) | ImGuiTreeNodeFlags.OpenOnArrow;
                bool showNode = !_showGraphicsLeafCharactersOnly || o->ChildObject != null || o->GetObjectType() == FFXIVClientStructs.FFXIV.Client.Graphics.Scene.ObjectType.CharacterBase;
                if (showNode && ImGui.TreeNodeEx(nodeText, nodeFlags))
                {
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    {
                        bool watched = _watchedRenderObjects.ContainsKey((IntPtr)o);
                        if (!watched)
                        {
                            int size = 0x80;
                            switch (o->GetObjectType())
                            {
                                case FFXIVClientStructs.FFXIV.Client.Graphics.Scene.ObjectType.CharacterBase:
                                    size = 0x8F0;
                                    break;
                                case FFXIVClientStructs.FFXIV.Client.Graphics.Scene.ObjectType.VfxObject:
                                    size = 0x1C8;
                                    break;
                            }
                            WatchObject(o, size);
                        }
                        else
                        {
                            _watchedRenderObjects.Remove((IntPtr)o);
                        }
                    }

                    if (o->ChildObject != null)
                        DrawSceneNode(o->ChildObject);
                    ImGui.TreePop();
                }
                o = o->NextSiblingObject;
            }
            while (o != start);
        }

        public unsafe void DrawWatchedMods()
        {
            if (ImGui.Button("Clear watch list"))
                _watchedRenderObjects.Clear();
            ImGui.SameLine();
            if (ImGui.Button("Clear modifications"))
                foreach (var v in _watchedRenderObjects)
                    v.Value.Modifications.Clear();

            if (_watchedRenderObjects.Count == 0)
                return;

            foreach (var v in _watchedRenderObjects)
            {
                v.Value.Live = false;
            }

            var root = FindSceneRoot();
            if (root != null)
                UpdateWatchedMods(root);

            List<IntPtr> del = new();
            foreach (var v in _watchedRenderObjects)
                if (!v.Value.Live)
                    del.Add(v.Key);
            foreach (var v in del)
                _watchedRenderObjects.Remove(v);

            ImGui.BeginTable("watched_graphics", 2);
            ImGui.TableSetupColumn("Ptr", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Data");
            ImGui.TableHeadersRow();
            foreach (var v in _watchedRenderObjects)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn(); ImGui.Text($"0x{v.Key:X}");
                ImGui.TableNextColumn(); DrawMods(v.Value);
            }
            ImGui.EndTable();

            Vector2 playerPos;
            if (Service.GameGui.WorldToScreen(Service.ClientState.LocalPlayer!.Position, out playerPos))
            {
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
                ImGuiHelpers.ForceNextWindowMainViewport();
                ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(0, 0));
                ImGui.Begin("arrow_overlay", ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground);
                ImGui.SetWindowSize(ImGui.GetIO().DisplaySize);

                foreach (var v in _watchedRenderObjects)
                {
                    var obj = (FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Object*)v.Key;
                    Vector2 objScreenPos;
                    if (!Service.GameGui.WorldToScreen(obj->Position, out objScreenPos))
                        continue;

                    ImGui.GetWindowDrawList().AddLine(playerPos, objScreenPos, 0xff0000ff);
                }

                ImGui.End();
                ImGui.PopStyleVar();
            }
        }

        public unsafe void WatchObject(void* o, int size)
        {
            if (_watchedRenderObjects.ContainsKey((IntPtr)o))
                return;

            var w = new WatchedRenderObject();
            for (int i = 0; i < size / 4; ++i)
                w.Data.Add(((uint*)o)[i]);
            _watchedRenderObjects.Add((IntPtr)o, w);
        }

        private unsafe void UpdateWatchedMods(FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Object* o)
        {
            var start = o;
            do
            {
                WatchedRenderObject? watch = _watchedRenderObjects.GetValueOrDefault((IntPtr)o);
                if (watch != null)
                    UpdateWatchedMod(o, watch);

                if (o->ChildObject != null)
                    UpdateWatchedMods(o->ChildObject);
                o = o->NextSiblingObject;
            }
            while (o != start);
        }

        private unsafe void UpdateWatchedMod(void* o, WatchedRenderObject w)
        {
            w.Live = true;

            int start = 0;
            for (int i = 0; i < w.Modifications.Count; ++i)
            {
                (int end, int nextStart) = w.Modifications[i];
                var mods = CheckUnmodRange((uint*)o, w, start, end);
                if (mods != null)
                {
                    w.Modifications.InsertRange(i, mods);
                    i += mods.Count;
                }
                start = nextStart;
            }

            var endMods = CheckUnmodRange((uint*)o, w, start, w.Data.Count);
            if (endMods != null)
                w.Modifications.AddRange(endMods);

            for (int i = 0; i < w.Data.Count; ++i)
                w.Data[i] = ((uint*)o)[i];
        }

        private unsafe List<(int, int)>? CheckUnmodRange(uint* o, WatchedRenderObject w, int start, int end)
        {
            while (start < end && o[start] == w.Data[start])
                ++start;
            if (start == end)
                return null; // nothing changed

            List<(int, int)> res = new();
            while (start < end)
            {
                int m = start + 1;
                while (m < end && o[m] != w.Data[m])
                    ++m;

                res.Add((start, m));
                start = m;
                while (start < end && o[start] == w.Data[start])
                    ++start;
            }
            return res;
        }

        private void DrawMods(WatchedRenderObject w)
        {
            var cn = ImGui.ColorConvertU32ToFloat4(0xff808080);
            var cm = ImGui.ColorConvertU32ToFloat4(0xff0000ff);
            int start = 0;
            var sb = new StringBuilder();
            foreach ((var end, var nextStart) in w.Modifications)
            {
                DrawHexString(w, ref start, end, cn, sb);
                DrawHexString(w, ref start, nextStart, cm, sb);
            }
            sb.Clear();
            DrawHexString(w, ref start, w.Data.Count, cn, sb);
        }

        private void DrawHexString(WatchedRenderObject w, ref int start, int end, Vector4 color, StringBuilder sb)
        {
            sb.Clear();
            while (start < end)
            {
                if (sb.Length > 0)
                    sb.Append(" ");
                sb.AppendFormat("{0:X8}", w.Data[start++]);

                if ((start % 16) == 0)
                {
                    ImGui.TextColored(color, sb.ToString());
                    sb.Clear();
                }
            }
            ImGui.TextColored(color, sb.ToString());
            ImGui.SameLine();
        }

        public unsafe void DrawMatrices()
        {
            if (Camera.Instance == null)
                return;

            ImGui.BeginTable("matrices", 2);
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Value");
            ImGui.TableHeadersRow();

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); ImGui.Text("VP");
            ImGui.TableNextColumn(); DrawMatrix(Camera.Instance.ViewProj);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); ImGui.Text("P");
            ImGui.TableNextColumn(); DrawMatrix(Camera.Instance.Proj);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); ImGui.Text("V");
            ImGui.TableNextColumn(); DrawMatrix(Camera.Instance.View);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); ImGui.Text("Camera Altitude");
            ImGui.TableNextColumn(); ImGui.Text(Utils.RadianString(Camera.Instance.CameraAltitude));

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); ImGui.Text("Camera Azimuth");
            ImGui.TableNextColumn(); ImGui.Text(Utils.RadianString(Camera.Instance.CameraAzimuth));

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); ImGui.Text("W");
            ImGui.TableNextColumn(); DrawMatrix(Camera.Instance.CameraWorld);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); ImGui.Text("Viewport size");
            ImGui.TableNextColumn(); ImGui.Text($"{Camera.Instance.ViewportSize.X:f6} {Camera.Instance.ViewportSize.Y:f6}");

            ImGui.EndTable();
        }

        private void DrawMatrix(SharpDX.Matrix mtx)
        {
            ImGui.Text($"{mtx[0]:f6} {mtx[1]:f6} {mtx[2]:f6} {mtx[3]:f6}");
            ImGui.Text($"{mtx[4]:f6} {mtx[5]:f6} {mtx[6]:f6} {mtx[7]:f6}");
            ImGui.Text($"{mtx[8]:f6} {mtx[9]:f6} {mtx[10]:f6} {mtx[11]:f6}");
            ImGui.Text($"{mtx[12]:f6} {mtx[13]:f6} {mtx[14]:f6} {mtx[15]:f6}");
        }

        public static unsafe FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Object* FindSceneRoot()
        {
            var player = Utils.GameObjectInternal(Service.ClientState.LocalPlayer);
            if (player == null || player->DrawObject == null)
                return null;

            var obj = &player->DrawObject->Object;
            while (obj->ParentObject != null)
                obj = obj->ParentObject;
            return obj;
        }

        public static unsafe void DumpScene()
        {
            var res = new StringBuilder("--- graphics scene dump ---");
            var root = FindSceneRoot();
            if (root != null)
            {
                DumpSceneNode(res, root, "");
            }
            Service.Log(res.ToString());
        }

        private static unsafe void DumpSceneNode(StringBuilder res, FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Object* o, string prefix)
        {
            var start = o;
            do
            {
                res.Append($"\n{prefix} {SceneNodeText(o)}");
                if (o->ChildObject != null)
                    DumpSceneNode(res, o->ChildObject, prefix + "-");
                o = o->NextSiblingObject;
            }
            while (o != start);
        }

        private static unsafe string SceneNodeText(FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Object* o)
        {
            var t = o->GetObjectType();
            var s = $"0x{(IntPtr)o:X}: t={t}, flags={Utils.SceneObjectFlags(o):X}, pos={Utils.Vec3String(o->Position)}, rot={Utils.QuatString(o->Rotation)}, scale={Utils.Vec3String(o->Scale)}";
            switch (t)
            {
                case FFXIVClientStructs.FFXIV.Client.Graphics.Scene.ObjectType.VfxObject:
                    s += $", ac={Utils.ReadField<int>(o, 0x128):X}, at={Utils.ReadField<int>(o, 0x130):X}, sc={Utils.ReadField<int>(o, 0x1B8):X}, st={Utils.ReadField<int>(o, 0x1C0)}:X";
                    break;
            }
            return s;
        }
    }
}