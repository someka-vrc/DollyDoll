using UnityEngine;

namespace Somekasu.DollyDoll
{
    public enum PBEasing
    {
        None,
        [InspectorName("正弦加速 - InSine")]
        InSine,
        [InspectorName("正弦減速 - OutSine")]
        OutSine,
        [InspectorName("正弦緩急 - InOutSine")]
        InOutSine,
        [InspectorName("四乗加速 - InQuart")]
        InQuart,
        [InspectorName("四乗減速 - OutQuart")]
        OutQuart,
        [InspectorName("四乗緩急 - InOutQuart")]
        InOutQuart,
        [InspectorName("指数加速 - InExpo")]
        InExpo,
        [InspectorName("指数減速 - OutExpo")]
        OutExpo,
        [InspectorName("指数緩急 - InOutExpo")]
        InOutExpo,
        [InspectorName("バウンド加速 - InBounce")]
        InBounce,
        [InspectorName("バウンド減速 - OutBounce")]
        OutBounce,
        [InspectorName("バウンド緩急 - InOutBounce")]
        InOutBounce,
    }
}