using UnityEngine;

namespace Somekasu.DollyDoll
{
    public enum PBMotionControl
    {
        ///<summary> Time-based animations allow you to pick from a list of ease presets. The overall duration of a time-based animation is the sum of point durations, minus the last one and anchors. </summary>
        [InspectorName("時間 - Time Based")]
        TimeBased,
        ///<summary> Speed-based animations allow you to create animations with custom ease curves. This can be useful for long animations where easing shouldn’t be applied uniformly across the entire path. </summary>
        [InspectorName("速度 - Speed Based")]
        SpeedBased
    }
}