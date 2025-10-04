using Microsoft.Xna.Framework;
using System;

namespace Snow.Engine
{
    public enum EaseType
    {
        Linear,
        EaseInQuad,
        EaseOutQuad,
        EaseInOutQuad,
        EaseInCubic,
        EaseOutCubic,
        EaseInOutCubic
    }

    public class Tween
    {
        public float Duration { get; set; }
        public float Elapsed { get; set; }
        public float StartValue { get; set; }
        public float EndValue { get; set; }
        public EaseType EaseType { get; set; }
        public bool IsComplete { get; set; }
        public Action OnComplete { get; set; }

        public Tween(float start, float end, float duration, EaseType easeType = EaseType.Linear)
        {
            StartValue = start;
            EndValue = end;
            Duration = duration;
            EaseType = easeType;
            Elapsed = 0f;
            IsComplete = false;
        }

        public void Update(float deltaTime)
        {
            if (IsComplete) return;

            Elapsed += deltaTime;
            if (Elapsed >= Duration)
            {
                Elapsed = Duration;
                IsComplete = true;
                OnComplete?.Invoke();
            }
        }

        public float GetValue()
        {
            if (Duration == 0f) return EndValue;

            float t = Elapsed / Duration;
            float easedT = ApplyEase(t, EaseType);
            return MathHelper.Lerp(StartValue, EndValue, easedT);
        }

        public void Reset()
        {
            Elapsed = 0f;
            IsComplete = false;
        }

        public static float ApplyEase(float t, EaseType easeType)
        {
            switch (easeType)
            {
                case EaseType.Linear:
                    return t;
                case EaseType.EaseInQuad:
                    return t * t;
                case EaseType.EaseOutQuad:
                    return t * (2f - t);
                case EaseType.EaseInOutQuad:
                    return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
                case EaseType.EaseInCubic:
                    return t * t * t;
                case EaseType.EaseOutCubic:
                    return (--t) * t * t + 1f;
                case EaseType.EaseInOutCubic:
                    return t < 0.5f ? 4f * t * t * t : (t - 1f) * (2f * t - 2f) * (2f * t - 2f) + 1f;
                default:
                    return t;
            }
        }
    }

    public class Vector2Tween
    {
        public float Duration { get; set; }
        public float Elapsed { get; set; }
        public Vector2 StartValue { get; set; }
        public Vector2 EndValue { get; set; }
        public EaseType EaseType { get; set; }
        public bool IsComplete { get; set; }
        public Action OnComplete { get; set; }

        public Vector2Tween(Vector2 start, Vector2 end, float duration, EaseType easeType = EaseType.Linear)
        {
            StartValue = start;
            EndValue = end;
            Duration = duration;
            EaseType = easeType;
            Elapsed = 0f;
            IsComplete = false;
        }

        public void Update(float deltaTime)
        {
            if (IsComplete) return;

            Elapsed += deltaTime;
            if (Elapsed >= Duration)
            {
                Elapsed = Duration;
                IsComplete = true;
                OnComplete?.Invoke();
            }
        }

        public Vector2 GetValue()
        {
            if (Duration == 0f) return EndValue;

            float t = Elapsed / Duration;
            float easedT = Tween.ApplyEase(t, EaseType);
            return Vector2.Lerp(StartValue, EndValue, easedT);
        }

        public void Reset()
        {
            Elapsed = 0f;
            IsComplete = false;
        }
    }
}
