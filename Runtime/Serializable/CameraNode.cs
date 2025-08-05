using Newtonsoft.Json;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Somekasu.DollyDoll
{
    [System.Serializable]
    public class SerializableVector3
    {
        public float X;
        public float Y;
        public float Z;

        public Vector3 ToVector3() => new Vector3(X, Y, Z);
        public Vector3 ToFloat3() => new float3(X, Y, Z);
        public static SerializableVector3 FromVector3(Vector3 vector) => new SerializableVector3 { X = vector.x, Y = vector.y, Z = vector.z };
    }

    /// <summary>
    /// JSONの構造体
    /// </summary>
    public class CameraNode
    {
        /// <summary>ローカル座標かどうか (FALSE=ワールド, TRUE=ローカル)</summary>
        public bool IsLocal = true; // FALSE=ワールド, TRUE=ローカル
        /// <summary>カメラ位置 (X,Y,Z)</summary>
        public SerializableVector3 Position = new SerializableVector3 { X = 0f, Y = 0f, Z = 0f };
        /// <summary>カメラ回転 (X,Y,Z)</summary>
        public SerializableVector3 Rotation = new SerializableVector3 { X = 0f, Y = 0f, Z = 0f };
        /// <summary>フォーカス距離 (Default: 1.5, Min: 0, Max: 10)</summary>
        public float FocalDistance = 1.5f;
        /// <summary>絞り値 (Default: 15, Min: 1.4, Max: 32)</summary>
        public float Aperture = 15f;
        /// <summary>グリーンスクリーン色相 (Hue) (Default: 120, Min: 0, Max: 360)</summary>
        public float Hue = 120f;
        /// <summary>グリーンスクリーン彩度 (Saturation) (Default: 100, Min: 0, Max: 100)</summary>
        public float Saturation = 100f;
        /// <summary>グリーンスクリーン輝度 (Lightness) (Default: 50, Min: 0, Max: 50)</summary>
        public float Lightness = 50f;
        /// <summary>Look-At-Me X方向オフセット (Default: 0, Min: -25, Max: 25)</summary>
        public float LookAtMeXOffset = 0f;
        /// <summary>Look-At-Me Y方向オフセット (Default: 0, Min: -25, Max: 25)</summary>
        public float LookAtMeYOffset = 0f;
        /// <summary>ズーム (Default: 45, Min: 20, Max: 150)</summary>
        public float Zoom = 45f;
        /// <summary>移動速度 (Default: 3, Min: 0.1, Max: 15)</summary>
        public float Speed = 3f;
        /// <summary>移動時間 (秒/m) (Default: 2, Min: 0.1, Max: 60)</summary>
        public float Duration = 2f;
        /// <summary>パス内のインデックス (Default: -1(append), Min: 0, Max: パス内ポイント数)</summary>
        public int Index = -1;
        /// <summary>パス番号 (Default: 現在のパス, Min: 0, Max: パス数)</summary>
        public int PathIndex = 0;
        [JsonIgnore]
        internal Vector3 PositionVector3 => Position != null ? Position.ToVector3() : Vector3.zero;
        [JsonIgnore]
        internal float3 PositionFloat3 => Position != null ? Position.ToFloat3() : float3.zero;
        [JsonIgnore]
        internal Vector3 RotationVector3
        {
            get
            {
            if (Rotation == null) return Vector3.zero;
            var rot = Rotation.ToVector3();
            return new Vector3(NormalizeAngle(rot.x), NormalizeAngle(rot.y), NormalizeAngle(rot.z));
            }
        }

        private float NormalizeAngle(float angle)
        {
            // UnityのTransform Inspectorに合わせて-180～180度に変換
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            if (angle < -180f) angle += 360f;
            return angle;
        }
    }
}