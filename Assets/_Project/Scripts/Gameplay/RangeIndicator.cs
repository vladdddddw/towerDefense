using UnityEngine;

namespace Vanguard.TD.Gameplay
{
    /// <summary>
    /// Draws a cyan ring around a turret to show reach. Single instance, owned
    /// by the bootstrapper.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RangeIndicator : MonoBehaviour
    {
        public static RangeIndicator Live { get; private set; }

        const int Segments = 72;
        LineRenderer _line;

        void Awake()
        {
            if (Live != null && Live != this) { Destroy(gameObject); return; }
            Live = this;

            _line = gameObject.AddComponent<LineRenderer>();
            _line.loop          = true;
            _line.useWorldSpace = true;
            _line.positionCount = Segments;
            _line.startWidth    = _line.endWidth = 0.06f;
            _line.sortingOrder  = 14;

            var sh = Shader.Find("Sprites/Default") ?? Shader.Find("Hidden/Internal-Colored");
            if (sh != null) _line.material = new Material(sh);

            // cyan gradient
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0f, 0.95f, 1f), 0f),
                    new GradientColorKey(new Color(0.5f, 0.4f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.9f, 0f),
                    new GradientAlphaKey(0.9f, 1f)
                });
            _line.colorGradient = grad;

            gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (Live == this) Live = null;
        }

        public void Display(Vector3 center, float radius)
        {
            gameObject.SetActive(true);
            for (int i = 0; i < Segments; i++)
            {
                float angle = 2f * Mathf.PI * i / Segments;
                _line.SetPosition(i, new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    center.y + Mathf.Sin(angle) * radius,
                    -0.05f));
            }
        }

        public void Conceal() => gameObject.SetActive(false);
    }
}
