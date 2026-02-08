using UnityEngine;

public class ParallaxMenu : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public RectTransform rect;
        public Vector2 speed = new Vector2(-10f, 0f);
        public Vector2 repeatSize = Vector2.zero;
    }

    public Layer[] layers;

    void Update()
    {
        if (layers == null) return;
        float dt = Time.unscaledDeltaTime;
        foreach (var layer in layers)
        {
            if (layer == null || layer.rect == null) continue;
            var pos = layer.rect.anchoredPosition;
            pos += layer.speed * dt;

            var size = layer.repeatSize;
            if (size == Vector2.zero)
            {
                size = layer.rect.rect.size;
            }

            if (size.x > 0f)
            {
                if (pos.x <= -size.x) pos.x += size.x;
                else if (pos.x >= size.x) pos.x -= size.x;
            }
            if (size.y > 0f)
            {
                if (pos.y <= -size.y) pos.y += size.y;
                else if (pos.y >= size.y) pos.y -= size.y;
            }

            layer.rect.anchoredPosition = pos;
        }
    }
}
