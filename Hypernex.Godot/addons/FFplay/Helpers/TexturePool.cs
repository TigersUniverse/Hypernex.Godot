using System;
using System.Collections.Generic;
using Godot;

namespace FFmpeg.Godot.Helpers
{
    public sealed class TexturePool : IDisposable
    {
        [Serializable]
        public class TexturePoolState
        {
            public bool inUse = false;
            public Image texture = null;
            public double pts;
        }

        public List<TexturePoolState> pool = new List<TexturePoolState>();
        public int index = 0;

        public TexturePool(int size)
        {
            pool.Capacity = size;
            for (int i = 0; i < size; i++)
            {
                pool.Add(new TexturePoolState()
                {
                    texture = Image.CreateEmpty(16, 16, false, Image.Format.Rgb8),
                });
            }
        }

        public TexturePoolState Get()
        {
            for (int i = 0; i < pool.Count && pool[index % pool.Count].inUse; i++)
                index++;
            if (pool[index % pool.Count].inUse)
            {
                // GD.Print($"Adding to texture pool {pool.Count}");
                var n = new TexturePoolState()
                {
                    texture = Image.CreateEmpty(16, 16, false, Image.Format.Rgb8),
                };
                pool.Add(n);
                index = pool.Count - 1;
            }
            pool[index % pool.Count].inUse = true;
            // if (GodotObject.IsInstanceValid(pool[index % pool.Count].texture))
            //     RenderingServer.FreeRid(pool[index % pool.Count].texture.GetRid());
            return pool[index % pool.Count];
        }

        public void Release(TexturePoolState state)
        {
            if (state == null)
                return;
            state.inUse = false;
            // if (GodotObject.IsInstanceValid(state.texture))
            //     RenderingServer.FreeRid(state.texture.GetRid());
            // state.texture = Image.Create(16, 16, false, Image.Format.Rgb8);
        }

        public void Dispose()
        {
            foreach (var tex in pool)
            {
                RenderingServer.FreeRid(tex.texture.GetRid());
            }
            pool.Clear();
        }
    }
}
