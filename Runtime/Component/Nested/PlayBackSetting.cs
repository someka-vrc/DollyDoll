using UnityEngine;
using System;

namespace Somekasu.DollyDoll
{
    [Serializable]
    public class PlayBackSetting
    {
        /// <summary>
        /// Motion control specifies whether the animation should be time- or speed-based.<br/>
        /// <list type="bullet">
        /// <item>When time-based, a Duration slider is shown</item>
        /// <item>When speed-based, the Fly Speed slider will be used</item>
        /// </list><br/>
        /// Speed-based animations allow you to create animations with custom ease curves. This can be useful for long animations where easing shouldn’t be applied uniformly across the entire path.<br/>
        /// Time-based animations allow you to pick from a list of ease presets. The overall duration of a time-based animation is the sum of point durations, minus the last one and anchors. So if there are three points, each with duration 2sec, the overall duration will be 4 seconds (A to B ~ 2sec, B to C ~ 2sec). The Easing configuration includes options for In, Out, and In-Out ease curves at different intensities.<br/>
        /// Read more about easing curves at easings.net.<br/>
        /// </summary>
        [SerializeField]
        internal PBMotionControl MotionControl = PBMotionControl.TimeBased;

        /// <summary>
        /// The easing curve to use for time-based animations. This is only used when MotionControl is set to TimeBased.
        /// </summary>
        [SerializeField]
        internal PBEasing Easing = PBEasing.None;

        /// <summary>
        /// The path type defines how the path is smoothed. The following path types are available.<br/>
        /// <list type="bullet">
        /// <item>Fitted paths use B-Spline interpolation and are very smooth, but may not pass through points</item>
        /// <item>Loose paths use Catmull Rom interpolation and pass through the points you’ve added, while being slightly less smooth than Fitted paths</item>
        /// <item>Linear paths are not smoothed</item>
        /// </list><br/>
        /// Fitted and Loose paths require at least 4 points to be smoothed. While a path has less than 4 points, it will always be linear.<br/>
        /// With at least 4 points, Fitted and Loose paths will be smoothed. When smoothed, a few additional elements come into play:<br/>
        /// <list type="bullet">
        /// <item>Anchors: The first and last points of a smoothed path are anchors. They are used to give shape to the beginning and end of the animation path. While anchors aren’t part of the path itself, they are used to interpolate values for the start and end points.</item>
        /// <item>Curve Points: Smoothed paths generate smaller “in-between” points between the points you add. These are used to visualize the path. You may also interact with a curve point to add a new point anywhere in an existing path.</item>
        /// </list>
        /// </summary>
        [SerializeField]
        internal PBPathType PathType = PBPathType.Fitted;

        /// <summary>
        /// The loop type specifies how the animation should loop. Options include Repeat, Reverse, and Revolve.<br/>
        /// <list type="bullet">
        /// <item>None means the animation will not loop</item>
        /// <item>Repeat looping restarts the path from the start once the camera reaches the end</item>
        /// <item>Reverse changes direction whenever the camera reaches the start or end of the path</item>
        /// <item>Revolve connects the end of a path back to the start so it loops smoothly</item>
        /// </list>
        /// </summary>
        [SerializeField]
        internal PBLooping Looping = PBLooping.None;
    }
}