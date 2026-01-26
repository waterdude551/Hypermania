using UnityEngine;
using MemoryPack;

namespace Game.Sim
{
    [MemoryPackable]
    public partial class LT_inputHistory
    {
        public GameInput[] buffer;
        public int front;
        public int count;

        // The structure of this input history follows a circular array / buffer, for constant access times to previos frames.
        // We only add on the last frame at the end, for constant O(1) time.
        public static LT_inputHistory Create()
        {
            LT_inputHistory history = new LT_inputHistory();
            history.buffer = new GameInput[64];
            history.front = 0;
            history.count = 0;
            return history;
        }
        public void push(GameInput input)
        {
            buffer[front] = input;
            front = (front + 1) % buffer.Length;
            if (count < buffer.Length)
            {
                count = count + 1;
            }
        }

        public GameInput getInput(int framesAgo)
        {
            if (framesAgo < 0 || framesAgo >= count)
            {
                return new GameInput(InputFlags.None);
            }
            int idx = (front - 1 - framesAgo + buffer.Length) % buffer.Length;
            return buffer[idx];
        }

        public bool wasPressed(InputFlags flag, int withinFrames)
        {
            if (withinFrames < 0 || withinFrames >= count)
            {
                return false;
            }
            for (int i = 0; i < withinFrames; i++)
            {
                if ((getInput(i).Flags & flag) == flag)
                {
                    return true;
                }
            }
            return false;
        }
    }
}