using UnityEngine;
using System;
namespace Somekasu.DollyDoll
{
    internal class Easing
    {
        internal enum Ease
        {
            // 緩急 弱 ↑
            // 一次関数
            Linear,

            // Sine
            InSine,
            OutSine,
            InOutSine,

            // 二次関数
            InQuad,
            OutQuad,
            InOutQuad,

            // 三次関数
            InCubic,
            OutCubic,
            InOutCubic,

            // 四次関数
            InQuart,
            OutQuart,
            InOutQuart,

            // 五次関数
            InQuint,
            OutQuint,
            InOutQuint,

            // 指数関数
            InExpo,
            OutExpo,
            InOutExpo,

            // 円形関数
            InCirc,
            OutCirc,
            InOutCirc,

            // 1度のみ振動
            InBack,
            OutBack,
            InOutBack,

            // 弾性
            InElastic,
            OutElastic,
            InOutElastic,

            // バウンド
            InBounce,
            OutBounce,
            InOutBounce
            // 緩急 強 ↓
        }

        // Func<in T, out TResult>(T arg);
        // イージング関数を返す関数
        internal static Func<float, float> GetEasingMethod(Ease ease)
        {
            switch (ease)
            {
                case Ease.Linear:
                    return Linear;

                case Ease.InSine:
                    return EaseInSine;
                case Ease.OutSine:
                    return EaseOutSine;
                case Ease.InOutSine:
                    return EaseInOutSine;

                case Ease.InQuad:
                    return EaseInQuad;
                case Ease.OutQuad:
                    return EaseOutQuad;
                case Ease.InOutQuad:
                    return EaseInOutQuad;

                case Ease.InCubic:
                    return EaseInCubic;
                case Ease.OutCubic:
                    return EasesOutCubic;
                case Ease.InOutCubic:
                    return EasesInOutCubic;

                case Ease.InQuart:
                    return EaseInQuart;
                case Ease.OutQuart:
                    return EaseOutQuart;
                case Ease.InOutQuart:
                    return EaseInOutQuart;

                case Ease.InQuint:
                    return EaseInQuint;
                case Ease.OutQuint:
                    return EaseOutQuint;
                case Ease.InOutQuint:
                    return EaseInOutQuint;

                case Ease.InExpo:
                    return EaseInExpo;
                case Ease.OutExpo:
                    return EaseOutExpo;
                case Ease.InOutExpo:
                    return EaseInOutExpo;

                case Ease.InCirc:
                    return EaseInCirc;
                case Ease.OutCirc:
                    return EaseOutCirc;
                case Ease.InOutCirc:
                    return EaseInOutCirc;

                case Ease.InBack:
                    return EaseInBack;
                case Ease.OutBack:
                    return EaseOutBack;
                case Ease.InOutBack:
                    return EaseInOutBack;

                case Ease.InElastic:
                    return EaseInElastic;
                case Ease.OutElastic:
                    return EaseOutElastic;
                case Ease.InOutElastic:
                    return EaseInOutElastic;

                case Ease.InBounce:
                    return EaseInBounce;
                case Ease.OutBounce:
                    return EaseOutBounce;
                case Ease.InOutBounce:
                    return EaseInOutBounce;

                default:
                    return Linear;
            }
        }

        internal static float Linear(float x)
        {
            return x;
        }

        internal static float EaseInSine(float x)
        {
            return 1.0f - Mathf.Cos((x * Mathf.PI) / 2.0f);
        }

        internal static float EaseOutSine(float x)
        {
            return Mathf.Sin((x * Mathf.PI) / 2.0f);
        }

        internal static float EaseInOutSine(float x)
        {
            return -(Mathf.Cos(Mathf.PI * x) - 1.0f) / 2.0f;
        }

        internal static float EaseInQuad(float x)
        {
            return x * x;
        }

        internal static float EaseOutQuad(float x)
        {
            return 1.0f - (1.0f - x) * (1.0f - x);
        }

        internal static float EaseInOutQuad(float x)
        {
            return x < 0.5f ? (2.0f * x * x) : (1.0f - Mathf.Pow(-2.0f * x + 2.0f, 2.0f) / 2.0f);
        }

        internal static float EaseInCubic(float x)
        {
            return x * x * x;
        }

        internal static float EasesOutCubic(float x)
        {
            return 1.0f - Mathf.Pow(1.0f - x, 3.0f);
        }

        internal static float EasesInOutCubic(float x)
        {
            return x < 0.5f ? (4.0f * x * x * x) : (1.0f - Mathf.Pow(-2.0f * x + 2.0f, 3.0f) / 2.0f);
        }

        internal static float EaseInQuart(float x)
        {
            return x * x * x * x;
        }

        internal static float EaseOutQuart(float x)
        {
            return 1.0f - Mathf.Pow(1.0f - x, 4.0f);
        }

        internal static float EaseInOutQuart(float x)
        {
            return x < 0.5f ? (8.0f * x * x * x * x) : (1.0f - Mathf.Pow(-2.0f * x + 2.0f, 4.0f) / 2.0f);
        }

        internal static float EaseInQuint(float x)
        {
            return x * x * x * x * x;
        }

        internal static float EaseOutQuint(float x)
        {
            return 1.0f - Mathf.Pow(1.0f - x, 5.0f);
        }

        internal static float EaseInOutQuint(float x)
        {
            return x < 0.5f ? (16.0f * x * x * x * x * x) : (1.0f - Mathf.Pow(-2.0f * x + 2.0f, 5.0f) / 2.0f);
        }

        internal static float EaseInExpo(float x)
        {
            return x == 0.0f ? (0.0f) : Mathf.Pow(2.0f, 10.0f * x - 10.0f);
        }

        internal static float EaseOutExpo(float x)
        {
            return x == 1.0f ? 1.0f : 1.0f - Mathf.Pow(2.0f, -10.0f * x);
        }

        internal static float EaseInOutExpo(float x)
        {
            return x == 0.0f ? 0.0f
            : x == 1.0f ? 1.0f
            : x < 0.5f ? Mathf.Pow(2.0f, 20.0f * x - 10.0f) / 2.0f
            : (2.0f - Mathf.Pow(2.0f, -20.0f * x + 10.0f)) / 2.0f;
        }

        internal static float EaseInCirc(float x)
        {
            return 1.0f - Mathf.Sqrt(1.0f - Mathf.Pow(x, 2.0f));
        }

        internal static float EaseOutCirc(float x)
        {
            return Mathf.Sqrt(1.0f - Mathf.Pow(x - 1.0f, 2.0f));
        }

        internal static float EaseInOutCirc(float x)
        {
            return x < 0.5f
            ? (1 - Mathf.Sqrt(1.0f - Mathf.Pow(2.0f * x, 2.0f))) / 2.0f
            : (Mathf.Sqrt(1.0f - Mathf.Pow(-2.0f * x + 2.0f, 2.0f)) + 1.0f) / 2.0f;
        }

        internal static float EaseInBack(float x)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1.0f;
            return c3 * x * x * x - c1 * x * x;
        }

        internal static float EaseOutBack(float x)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1.0f;
            return 1.0f + c3 * Mathf.Pow(x - 1.0f, 3.0f) + c1 * Mathf.Pow(x - 1.0f, 2.0f);
        }

        internal static float EaseInOutBack(float x)
        {
            float c1 = 1.70158f;
            float c2 = c1 * 1.525f;
            return x < 0.5f
            ? (Mathf.Pow(2.0f * x, 2.0f) * ((c2 + 1.0f) * 2.0f * x - c2)) / 2.0f
            : (Mathf.Pow(2.0f * x - 2.0f, 2.0f) * ((c2 + 1.0f) * (x * 2.0f - 2.0f) + c2) + 2.0f) / 2.0f;
        }

        internal static float EaseInElastic(float x)
        {
            float c4 = (2.0f * Mathf.PI) / 3.0f;
            return x == 0.0f
            ? 0.0f
            : x == 1.0f
            ? 1.0f
            : -Mathf.Pow(2.0f, 10.0f * x - 10.0f) * Mathf.Sin((x * 10.0f - 10.75f) * c4);
        }

        internal static float EaseOutElastic(float x)
        {
            float c4 = (2.0f * Mathf.PI) / 3.0f;
            return x == 0.0f
            ? 0.0f
            : x == 1.0f
            ? 1.0f
            : Mathf.Pow(2.0f, -10.0f * x) * Mathf.Sin((x * 10.0f - 0.75f) * c4) + 1.0f;
        }

        internal static float EaseInOutElastic(float x)
        {
            float c5 = (2.0f * Mathf.PI) / 4.5f;
            return x == 0.0f
            ? 0.0f
            : x == 1.0f
            ? 1.0f
            : x < 0.5f
            ? -(Mathf.Pow(2.0f, 20.0f * x - 10.0f) * Mathf.Sin((20.0f * x - 11.125f) * c5)) / 2.0f
            : (Mathf.Pow(2.0f, -20.0f * x + 10.0f) * Mathf.Sin((20.0f * x - 11.125f) * c5)) / 2.0f + 1.0f;
        }

        internal static float EaseInBounce(float x)
        {
            return 1.0f - EaseOutBounce(1.0f - x);
        }

        internal static float EaseOutBounce(float x)
        {
            float a = 7.5625f;
            float b = 2.75f;
            if (x < 1.0f / b)
            {
                return a * x * x;
            }
            else if (x < 2.0f / b)
            {
                float c = (x - 1.5f / b);
                return a * c * c + 0.75f;
            }
            else if (x < 2.5 / b)
            {
                float c = (x - 2.25f / b);
                return a * c * c + 0.9375f;
            }
            else
            {
                float c = (x - 2.625f / b);
                return a * c * c + 0.984375f;
            }
        }

        internal static float EaseInOutBounce(float x)
        {
            return x < 0.5f
            ? (1.0f - EaseOutBounce(1.0f - 2.0f * x)) / 2.0f
            : (1.0f + EaseOutBounce(2.0f * x - 1.0f)) / 2.0f;
        }
    }


}