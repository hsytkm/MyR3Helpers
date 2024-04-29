using System.ComponentModel;
using System.Runtime.CompilerServices;
using ObservableCollections;
using R3;

namespace MyR3Helpers;

// R3 で提供されていない ReactiveProperty.ObserveElementProperty() を移植しました。
// 移植コードの詳細を把握していませんが、基本動作は問題ないと思っています。

// [MVVM をリアクティブプログラミングで快適に ReactiveProperty オーバービュー 2020 年版 後編 #C# - Qiita](https://qiita.com/okazuki/items/6faac7cb1a7d8a6ad0f2#observeelementproperty)

public static partial class R3Helpers
{
    /// <summary>
    /// Observe collection element's property.
    /// </summary>
    /// <typeparam name="TElement">Type of element</typeparam>
    /// <typeparam name="TProperty">Type of property</typeparam>
    /// <param name="source">Data source</param>
    /// <param name="propertySelector">Property selection expression</param>
    /// <param name="pushCurrentValueOnSubscribe">Push current value on first subscribe</param>
    /// <returns>Property value sequence</returns>
    public static Observable<PropertyPack<TElement, TProperty>> ObserveElementProperty<TElement, TProperty>(
        this IObservableCollection<TElement> source,
        Func<TElement, TProperty> propertySelector,
        bool pushCurrentValueOnSubscribe = true,
        [CallerArgumentExpression(nameof(propertySelector))] string? propertySelectorExpr = null)
        where TElement : class, INotifyPropertyChanged
    {
        ArgumentNullException.ThrowIfNull(nameof(propertySelectorExpr));
        var propertyName = propertySelectorExpr![(propertySelectorExpr!.LastIndexOf('.') + 1)..];

        return CollectionUtilities.ObserveElementProperty(source, propertySelector, propertyName, pushCurrentValueOnSubscribe);
    }
}

static file class CollectionUtilities
{
    /// <summary>
    /// Observe collection element's property.
    /// </summary>
    /// <typeparam name="TCollection">Type of collection</typeparam>
    /// <typeparam name="TElement">Type of element</typeparam>
    /// <typeparam name="TProperty">Type of property</typeparam>
    /// <param name="source">Data source</param>
    /// <param name="propertySelector">Property selection expression</param>
    /// <param name="propertyName">Property name</param>
    /// <param name="pushCurrentValueOnSubscribe">Push current value on first subscribe</param>
    /// <returns>Property value sequence</returns>
    public static Observable<PropertyPack<TElement, TProperty>> ObserveElementProperty<TCollection, TElement, TProperty>(
        TCollection source,
        Func<TElement, TProperty> propertySelector,
        string propertyName,
        bool pushCurrentValueOnSubscribe = true)
        where TCollection : IObservableCollection<TElement>
        where TElement : class, INotifyPropertyChanged
    {
        return ObserveElementCore<TCollection, TElement, PropertyPack<TElement, TProperty>>
        (
            source,
            (x, observer) => x.ObservePropertyLegacy(propertySelector, propertyName, pushCurrentValueOnSubscribe)
                .Subscribe(y =>
                {
                    var pair = PropertyPack.Create(x, propertyName, y);
                    observer.OnNext(pair);
                })
        );
    }

    /// <summary>
    /// Core logic of ObserveElementXXXXX methods.
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <typeparam name="TElement">Type of element.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="source">source collection</param>
    /// <param name="subscribeAction">element subscribe logic.</param>
    /// <returns></returns>
    private static Observable<TResult> ObserveElementCore<TCollection, TElement, TResult>(
        TCollection source,
        Func<TElement, Observer<TResult>, IDisposable> subscribeAction)
        where TCollection : IObservableCollection<TElement>
        where TElement : class
    {
        return Observable.Create<TResult>(observer =>
        {
            //--- cache element property subscriptions
            Dictionary<object, IDisposable> subscriptionCache = [];

            //--- subscribe / unsubscribe property which all elements have
            void subscribe(TElement x)
            {
                var subscription = subscribeAction(x, observer);
                subscriptionCache.Add(x, subscription);
            }
            void unsubscribeAll()
            {
                foreach (var x in subscriptionCache.Values)
                {
                    x.Dispose();
                }
                subscriptionCache.Clear();
            }

            foreach (var x in source)
            {
                subscribe(x);
            }

            //--- hook collection changed
            var d1 = source.ObserveRemove().Select(x => x.Value)
                .Merge(source.ObserveReplace().Select(x => x.OldValue))
                .Subscribe(x =>
                {
                    subscriptionCache[x].Dispose();
                    subscriptionCache.Remove(x);
                });
            var d2 = source.ObserveAdd().Select(x => x.Value)
                .Merge(source.ObserveReplace().Select(x => x.NewValue))
                .Subscribe(x => subscribe(x));
            var d3 = source.ObserveReset()
                .Subscribe(_ =>
                {
                    unsubscribeAll();
                    foreach (var x in source)
                    {
                        subscribe(x);
                    }
                });

            //--- unsubscribe
            return Disposable.Create(() =>
            {
                Disposable.Combine(d1, d2, d3).Dispose();
                unsubscribeAll();
            });
        });
    }
}

static file class InternalObservable
{
    /// <summary>
    /// Converts NotificationObject's property changed to an observable sequence.
    /// </summary>
    /// <typeparam name="TSubject">The type of the subject.</typeparam>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <param name="subject">The subject.</param>
    /// <param name="propertySelector">Argument is self, Return is target property.</param>
    /// <param name="propertyName">Property name</param>
    /// <param name="pushCurrentValueOnSubscribe">Push current value on first subscribe.</param>
    /// <returns></returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Observable<TProperty> ObservePropertyLegacy<TSubject, TProperty>(
        this TSubject subject,
        Func<TSubject, TProperty> propertySelector,
        string propertyName,
        bool pushCurrentValueOnSubscribe = true)
        where TSubject : INotifyPropertyChanged
    {
        // ◆ネストしたプロパティは未移植
        //return ExpressionTreeUtils.IsNestedPropertyPath(propertySelector) ?
        //    ObserveNestedPropertyLegacy(subject, propertySelector, pushCurrentValueOnSubscribe) :
        //    ObserveSimplePropertyLegacy(subject, propertySelector, pushCurrentValueOnSubscribe);

        return ObserveSimplePropertyLegacy(subject, propertySelector, propertyName, pushCurrentValueOnSubscribe);
    }

    private static Observable<TProperty> ObserveSimplePropertyLegacy<TSubject, TProperty>(
        this TSubject subject,
        Func<TSubject, TProperty> propertySelector,
        string propertyName,
        bool pushCurrentValueOnSubscribe = true)
        where TSubject : INotifyPropertyChanged
    {
        var isFirst = true;

        //return Observable.Defer(() =>
        {
            var flag = isFirst;
            isFirst = false;

            var q = subject.PropertyChangedAsObservableLegacy()
                .Where(e => e.PropertyName == propertyName || string.IsNullOrEmpty(e.PropertyName))
                .Select(_ => propertySelector(subject));

            return (pushCurrentValueOnSubscribe && flag) ? q.Prepend(propertySelector(subject)) : q;
        }
        //);
    }

    /// <summary>
    /// Converts PropertyChanged to an observable sequence.
    /// </summary>
    private static Observable<PropertyChangedEventArgs> PropertyChangedAsObservableLegacy<T>(this T subject)
        where T : INotifyPropertyChanged
    {
        return Observable.FromEvent<PropertyChangedEventHandler, PropertyChangedEventArgs>(
            h => (sender, e) => h(e),
            h => subject.PropertyChanged += h,
            h => subject.PropertyChanged -= h);
    }
}