using System;
using UniRx;
using UnityEditor;
using UnityEngine.UIElements;

namespace Somekasu.DollyDoll
{
    internal static class UIElementsExtensions
    {
        /// <summary>
        /// DropdownFieldの選択変更を指定したenumフィールドにActionで反映し、Undo履歴も記録します。
        /// </summary>
        /// <typeparam name="TEnum">反映するenum型</typeparam>
        /// <param name="dropdown">対象のDropdownField</param>
        /// <param name="setEnum">enumフィールドへ値をセットするAction</param>
        /// <param name="context">Undo対象のUnityオブジェクト</param>
        /// <param name="historyName">Undo履歴名</param>
        /// <returns>購読解除用IDisposable</returns>
        public static IDisposable CopyToEnumOnDropdownChanged<TEnum>(this DropdownField dropdown, Action<TEnum> setEnum, UnityEngine.Object context, string historyName = "enum property") where TEnum : struct, Enum
        {
            return Observable.FromEvent<EventCallback<ChangeEvent<string>>, ChangeEvent<string>>(
                h => new EventCallback<ChangeEvent<string>>(h),
                h => dropdown.RegisterValueChangedCallback(h),
                h => dropdown.UnregisterValueChangedCallback(h))
                .Select(_ => dropdown.index)
                .Subscribe(idx =>
                {
                    var enumValues = (TEnum[])Enum.GetValues(typeof(TEnum));
                    if (idx >= 0 && idx < enumValues.Length)
                    {
                        Undo.SetCurrentGroupName($"Edit {historyName}");
                        Undo.RecordObject(context, $"Edit {historyName} record");
                        setEnum(enumValues[idx]);
                        // EditorUtility.SetDirty(context);
                    }
                });
        }

        /// <summary>
        /// UIElementsの値変更イベントをIObservableとして取得します。
        /// </summary>
        /// <typeparam name="T">値の型</typeparam>
        /// <param name="source">値変更通知を持つUI要素</param>
        /// <returns>変更後の値を通知するIObservable</returns>
        public static IObservable<T> OnValueChangedAsObservable<T>(this INotifyValueChanged<T> source)
        {
            return Observable.FromEvent<EventCallback<ChangeEvent<T>>, ChangeEvent<T>>(
                h => new EventCallback<ChangeEvent<T>>(h),
                h => source.RegisterValueChangedCallback(h),
                h => source.UnregisterValueChangedCallback(h))
                .Select(x => x.newValue);
        }

        /// <summary>
        /// UIElementsの値変更をReactivePropertyに反映し、Undo履歴も記録します。
        /// </summary>
        /// <typeparam name="T">値の型</typeparam>
        /// <param name="elem">値変更通知を持つUI要素</param>
        /// <param name="rp">反映先のReactiveProperty</param>
        /// <param name="context">Undo対象のUnityオブジェクト</param>
        /// <param name="historyName">Undo履歴名</param>
        /// <returns>購読解除用IDisposable</returns>
        public static IDisposable CopyToReactiveOnValueChanged<T>(this INotifyValueChanged<T> elem, ReactiveProperty<T> rp, UnityEngine.Object context, string historyName = "property")
        {
            elem.value = rp.Value;
            return elem.OnValueChangedAsObservable()
                .Subscribe(value =>
                {
                    Undo.SetCurrentGroupName($"Edit {historyName}");
                    Undo.RecordObject(context, $"Edit {historyName} record");
                    rp.Value = value;
                    // EditorUtility.SetDirty(context);
                });
        }
        
        /// <summary>
        /// VisualElementのクリックイベントをIObservableとして取得します。
        /// </summary>
        /// <param name="source">クリックイベントを持つUI要素</param>
        /// <returns>ClickEventを通知するIObservable</returns>
        public static IObservable<ClickEvent> OnClickedAsObservable(this VisualElement source)
        {
            return Observable.FromEvent<EventCallback<ClickEvent>, ClickEvent>(
                h => new EventCallback<ClickEvent>(h),
                h => source.RegisterCallback(h),
                h => source.UnregisterCallback(h));
        }

        /// <summary>
        /// IObservableの値をTextElementのtextプロパティに反映します。
        /// </summary>
        /// <typeparam name="T">値の型</typeparam>
        /// <param name="source">値を通知するIObservable</param>
        /// <param name="text">反映先のTextElement</param>
        /// <returns>購読解除用IDisposable</returns>
        public static IDisposable SubscribeToText<T>(this IObservable<T> source, TextElement text)
        {
            return source.SubscribeWithState(text, (x, t) => t.text = x.ToString());
        }
    }
}