using System;
using UnityEditor;
using UnityEngine;

namespace Design.Animation.Editors
{
    [Serializable]
    public sealed class MoveBuilderModel
    {
        public GameObject CharacterPrefab;
        public AnimationClip Clip;
        public HitboxData Data;

        public int CurrentTick;
        public int SelectedBoxIndex = -1;
        private bool _hasUnsavedChanges;

        public bool HasUnsavedChanges => _hasUnsavedChanges;

        public event Action Changed;

        public bool HasAllInputs => CharacterPrefab && Clip && Data;
        public int TotalTicks => Data ? Mathf.Max(1, Data.TotalTicks) : 1;

        public float CurrentTimeSeconds(int tps) => CurrentTick / (float)Mathf.Max(1, tps);

        #region Data Binding
        public void BindDataToClipLength(MoveBuilderModel model, int tps)
        {
            if (!Data || !Clip)
                return;

            Data.Clip = Clip;

            float length = Mathf.Max(0.0001f, Clip.length);
            int totalTicks = Mathf.Max(1, Mathf.CeilToInt(length * Mathf.Max(1, tps)));
            Data.EnsureSize(totalTicks);

            CurrentTick = Mathf.Clamp(CurrentTick, 0, totalTicks - 1);
            SelectedBoxIndex = Mathf.Clamp(SelectedBoxIndex, -1, GetCurrentFrame()?.Boxes.Count - 1 ?? -1);

            MarkDirty();
            model.SaveAsset();
        }
        #endregion

        #region Modifications
        public void SetTick(int tick)
        {
            if (!Data)
                return;
            CurrentTick = Mathf.Clamp(tick, 0, TotalTicks - 1);
        }

        public FrameData GetCurrentFrame()
        {
            if (!Data)
                return null;
            return Data.GetFrame(CurrentTick);
        }

        public void SelectBox(int index)
        {
            var frame = GetCurrentFrame();
            int max = frame != null ? frame.Boxes.Count - 1 : -1;
            SelectedBoxIndex = Mathf.Clamp(index, -1, max);
        }

        public void AddBox(HitboxKind kind)
        {
            var frame = GetCurrentFrame();
            if (frame == null)
                return;

            RecordUndo("Add Box");

            var b = new BoxData
            {
                Name = kind == HitboxKind.Hitbox ? "Hit" : "Hurt",
                CenterLocal = Vector2.zero,
                SizeLocal = new Vector2(0.5f, 0.5f),
                Props = new BoxProps { Kind = kind, HitstunTicks = kind == HitboxKind.Hitbox ? 12 : 0 },
            };

            frame.Boxes.Add(b);
            SelectedBoxIndex = frame.Boxes.Count - 1;

            MarkDirty();
        }

        public void DuplicateSelected()
        {
            var frame = GetCurrentFrame();
            if (frame == null)
                return;
            if (SelectedBoxIndex < 0 || SelectedBoxIndex >= frame.Boxes.Count)
                return;

            RecordUndo("Duplicate Box");

            var copy = frame.Boxes[SelectedBoxIndex];
            copy.Name += "_Copy";
            frame.Boxes.Add(copy);

            SelectedBoxIndex = frame.Boxes.Count - 1;

            MarkDirty();
        }

        public void DeleteSelected()
        {
            var frame = GetCurrentFrame();
            if (frame == null)
                return;
            if (SelectedBoxIndex < 0 || SelectedBoxIndex >= frame.Boxes.Count)
                return;

            RecordUndo("Delete Box");

            frame.Boxes.RemoveAt(SelectedBoxIndex);
            SelectedBoxIndex = -1;

            MarkDirty();
        }

        public void MoveBoxCenter(int index, Vector2 newCenterLocal)
        {
            var frame = GetCurrentFrame();
            if (frame == null)
                return;
            if (index < 0 || index >= frame.Boxes.Count)
                return;

            var b = frame.Boxes[index];
            if (b.CenterLocal == newCenterLocal)
                return;

            RecordUndo("Move Box");

            b.CenterLocal = newCenterLocal;
            frame.Boxes[index] = b;

            MarkDirty();
        }

        public void SetBox(int index, BoxData updated)
        {
            var frame = GetCurrentFrame();
            if (frame == null)
                return;
            if (index < 0 || index >= frame.Boxes.Count)
                return;

            var cur = frame.Boxes[index];
            if (cur == updated)
                return;

            RecordUndo("Edit Box");

            frame.Boxes[index] = updated;

            MarkDirty();
        }

        public void SetBoxesFromPreviousFrame()
        {
            if (!Data)
                return;
            if (CurrentTick <= 0)
                return;

            var prev = Data.GetFrame(CurrentTick - 1);
            var cur = Data.GetFrame(CurrentTick);
            if (prev == null || cur == null)
                return;

            RecordUndo("Copy Boxes From Previous Frame");

            cur.Boxes.Clear();

            for (int i = 0; i < prev.Boxes.Count; i++)
            {
                var src = prev.Boxes[i];

                var dst = new BoxData
                {
                    Name = src.Name,
                    CenterLocal = src.CenterLocal,
                    SizeLocal = src.SizeLocal,
                    Props = src.Props,
                };

                cur.Boxes.Add(dst);
            }

            if (SelectedBoxIndex >= cur.Boxes.Count)
                SelectedBoxIndex = cur.Boxes.Count - 1;
            if (cur.Boxes.Count == 0)
                SelectedBoxIndex = -1;

            MarkDirty();
        }
        #endregion

        #region Helpers
        public void SaveAsset()
        {
            if (!Data)
                return;

            EditorUtility.SetDirty(Data);
            AssetDatabase.SaveAssets();

            _hasUnsavedChanges = false;
        }

        private void MarkDirty()
        {
            if (Data)
            {
                EditorUtility.SetDirty(Data);
                _hasUnsavedChanges = true;
            }
        }

        private void RecordUndo(string label)
        {
            // Important: Undo needs to record the ScriptableObject (Data), not the frame/box contents.
            if (Data)
                Undo.RecordObject(Data, label);
        }
        #endregion
    }
}
