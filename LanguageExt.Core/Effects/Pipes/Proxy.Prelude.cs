using System;
using LanguageExt.Common;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using LanguageExt.Effects.Traits;
using static LanguageExt.Prelude;

namespace LanguageExt.Pipes;

/// <summary>
/// The `static Proxy` class is the `Prelude` of the Pipes system.
/// </summary>
public static class Proxy
{
    internal const MethodImplOptions mops = MethodImplOptions.AggressiveInlining;

    /// <summary>
    /// Wait for a value to flow from upstream (whilst in a `Pipe` or a `Consumer`)
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Consumer<A, A> awaiting<A>() =>
        PureProxy.ConsumerAwait<A>();

    /// <summary>
    /// Send a value flowing downstream (whilst in a `Producer` or a `Pipe`)
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Producer<A, Unit> yield<A>(A value) =>
        PureProxy.ProducerYield(value);

    /// <summary>
    /// Create a queue
    /// </summary>
    /// <remarks>A `Queue` is a `Producer` with an `Enqueue`, and a `Done` to cancel the operation</remarks>
    [Pure, MethodImpl(mops)]
    public static Queue<RT, A, Unit> Queue<RT, A>() where RT : HasIO<RT, Error>
    {
        var c = new Channel<A>();
        var p = Producer.yieldAll<RT, A>(c);
        return new Queue<RT, A, Unit>(p, c);
    }

    /// <summary>
    /// Create a `Producer` from an `IEnumerable`.  This will automatically `yield` each value of the
    /// `IEnumerable` down stream
    /// </summary>
    /// <param name="xs">Items to `yield`</param>
    /// <typeparam name="X">Type of the value to `yield`</typeparam>
    /// <returns>`Producer`</returns>
    [Pure, MethodImpl(mops)]
    public static Producer<X, Unit> yieldAll<X>(IEnumerable<X> xs) =>
        from x in many(xs)
        from _ in PureProxy.ProducerYield<X>(x)
        select unit;

    /// <summary>
    /// Create a `Producer` from an `IAsyncEnumerable`.  This will automatically `yield` each value of the
    /// `IEnumerable` down stream
    /// </summary>
    /// <param name="xs">Items to `yield`</param>
    /// <typeparam name="X">Type of the value to `yield`</typeparam>
    /// <returns>`Producer`</returns>
    [Pure, MethodImpl(mops)]
    public static Producer<X, Unit> yieldAll<X>(IAsyncEnumerable<X> xs) =>
        from x in many(xs)
        from _ in PureProxy.ProducerYield<X>(x)
        select unit;

    /// <summary>
    /// Create a `Producer` from an `IObservable`.  This will automatically `yield` each value of the
    /// `IObservable` down stream
    /// </summary>
    /// <param name="xs">Items to `yield`</param>
    /// <typeparam name="X">Type of the value to `yield`</typeparam>
    /// <returns>`Producer`</returns>
    [Pure, MethodImpl(mops)]
    public static Producer<X, Unit> yieldAll<X>(IObservable<X> xs) =>
        from x in many(xs)
        from _ in PureProxy.ProducerYield<X>(x)
        select unit;


    /// <summary>
    /// Repeat the `Producer` indefinitely
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Producer<RT, OUT, Unit> repeat<RT, OUT, R>(Producer<RT, OUT, R> ma) where RT : HasIO<RT, Error> =>
        from _ in many(units)
        from x in ma
        select unit;

    /// <summary>
    /// Repeat the `Consumer` indefinitely
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Consumer<RT, IN, Unit> repeat<RT, IN, R>(Consumer<RT, IN, R> ma) where RT : HasIO<RT, Error> =>
        from _ in many(units)
        from x in ma
        select unit;

    /// <summary>
    /// Repeat the `Pipe` indefinitely
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Pipe<RT, IN, OUT, Unit> repeat<RT, IN, OUT, R>(Pipe<RT, IN, OUT, R> ma) where RT : HasIO<RT, Error> =>
        from _ in many(units)
        from x in ma
        select unit;

    static IEnumerable<Unit> units
    {
        get
        {
            while (true)
            {
                yield return default;
            }
        }
    }

    /// <summary>
    /// Lift an IO monad into the `Proxy` monad transformer
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Proxy<RT, A1, A, B1, B, R> lift<RT, A1, A, B1, B, R>(Eff<R> ma) where RT : HasIO<RT, Error> =>
        new M<RT, A1, A, B1, B, R>(ma.Map(Pure<RT, A1, A, B1, B, R>).WithRuntime<RT>().Morphism);

    /// <summary>
    /// Lift an IO monad into the `Proxy` monad transformer
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Proxy<RT, A1, A, B1, B, R> lift<RT, A1, A, B1, B, R>(Eff<RT, R> ma) where RT : HasIO<RT, Error> =>
        new M<RT, A1, A, B1, B, R>(ma.Map(Pure<RT, A1, A, B1, B, R>).Morphism);

    /// <summary>
    /// Lift an IO monad into the `Proxy` monad transformer
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Proxy<RT, A1, A, B1, B, R> lift<RT, A1, A, B1, B, R>(Transducer<RT, R> ma) 
        where RT : HasIO<RT, Error> =>
        new M<RT, A1, A, B1, B, R>(ma.Map(Pure<RT, A1, A, B1, B, R>));

    /// <summary>
    /// Lift a transducer into the `Proxy` monad transformer
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Proxy<RT, A1, A, B1, B, R> lift<RT, A1, A, B1, B, R>(Transducer<RT, Sum<Error, R>> ma) 
        where RT : HasIO<RT, Error> =>
        new M<RT, A1, A, B1, B, R>(ma.MapRight(Pure<RT, A1, A, B1, B, R>));

    /// <summary>
    /// Lift a transducer into the `Proxy` monad transformer
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Proxy<RT, A1, A, B1, B, R> lift<RT, A1, A, B1, B, R>(Transducer<Unit, R> ma) 
        where RT : HasIO<RT, Error> =>
        new M<RT, A1, A, B1, B, R>(
            Transducer.compose(Transducer.constant<RT, Unit>(default), ma.Map(Pure<RT, A1, A, B1, B, R>)));

    /// <summary>
    /// Lift a transducer into the `Proxy` monad transformer
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Proxy<RT, A1, A, B1, B, R> lift<RT, A1, A, B1, B, R>(Transducer<Unit, Sum<Error, R>> ma)
        where RT : HasIO<RT, Error> =>
        new M<RT, A1, A, B1, B, R>(
            Transducer.compose(Transducer.constant<RT, Unit>(default), ma.MapRight(Pure<RT, A1, A, B1, B, R>)));

    internal static Unit dispose<A>(A d) where A : IDisposable
    {
        d.Dispose();
        return default;
    }

    internal static Unit anyDispose<A>(A x)
    {
        if (x is IDisposable d)
        {
            d.Dispose();
        }

        return default;
    }

    /// <summary>
    /// The identity `Pipe`, simply replicates its upstream value and propagates it downstream 
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Pipe<RT, A, A, R> cat<RT, A, R>() where RT : HasIO<RT, Error> =>
        pull<RT, Unit, A, R>(default).ToPipe();

    /// <summary>
    /// Forward requests followed by responses
    ///
    ///    pull = request | respond | pull
    /// 
    /// </summary>
    /// <remarks>
    /// `pull` is the identity of the pull category.
    /// </remarks>
    [Pure, MethodImpl(mops)]
    public static Proxy<RT, UOut, UIn, UOut, UIn, A> pull<RT, UOut, UIn, A>(UOut a1)
        where RT : HasIO<RT, Error> =>
        new Request<RT, UOut, UIn, UOut, UIn, A>(a1,
                                                 a => new Respond<RT, UOut, UIn, UOut, UIn, A>(a,
                                                     pull<RT, UOut, UIn, A>));

    /// <summary>
    /// `push = respond | request | push`
    /// </summary>
    /// <remarks>
    /// `push` is the identity of the push category.
    /// </remarks>
    [Pure, MethodImpl(mops)]
    public static Proxy<RT, UOut, UIn, UOut, UIn, A> push<RT, UOut, UIn, A>(UIn a) 
        where RT : HasIO<RT, Error> =>
        new Respond<RT, UOut, UIn, UOut, UIn, A>(a, 
                                                 a1 => new Request<RT, UOut, UIn, UOut, UIn, A>(a1, 
                                                     push<RT, UOut, UIn, A>));

    /// <summary>
    /// Send a value of type `DOut` downstream and block waiting for a reply of type `DIn`
    /// </summary>
    /// <remarks>
    /// `respond` is the identity of the respond category.
    /// </remarks>
    [Pure, MethodImpl(mops)]
    public static Proxy<RT, X1, X, DIn, DOut, DIn> respond<RT, X1, X, DIn, DOut>(DOut value) 
        where RT : HasIO<RT, Error> =>
        new Respond<RT, X1, X, DIn, DOut, DIn>(value, r => new Pure<RT, X1, X, DIn, DOut, DIn>(r));

    /// <summary>
    /// Send a value of type `UOut` upstream and block waiting for a reply of type `UIn`
    /// </summary>
    /// <remarks>
    /// `request` is the identity of the request category.
    /// </remarks>
    [Pure, MethodImpl(mops)]
    public static Proxy<RT, UOut, UIn, Y1, Y, UIn> request<RT, UOut, UIn, Y1, Y>(UOut value) 
        where RT : HasIO<RT, Error> =>
        new Request<RT, UOut, UIn, Y1, Y, UIn>(value, r => new Pure<RT, UOut, UIn, Y1, Y, UIn>(r));


    /// <summary>
    /// `reflect` transforms each streaming category into its dual:
    ///
    /// The request category is the dual of the respond category
    ///
    ///      reflect . respond = request
    ///      reflect . (f | g) = reflect . f | reflect . g
    ///      reflect . request = respond
    ///      reflect . (f | g) = reflect . f | reflect . g
    ///
    /// The pull category is the dual of the push category
    ///
    ///      reflect . push = pull
    ///      reflect . (f | g) = reflect . f | reflect . g
    ///      reflect . pull = push
    ///      reflect . (f | g) = reflect . f | reflect . g
    ///
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Proxy<RT, DOut, DIn, UIn, UOut, R> reflect<RT, UOut, UIn, DIn, DOut, R>(
        Proxy<RT, UOut, UIn, DIn, DOut, R> p)
        where RT : HasIO<RT, Error> =>
        p.Reflect();

    /// <summary>
    /// `p.ForEach(body)` loops over the `Producer p` replacing each `yield` with `body`
    /// 
    ///     Producer b r -> (b -> Producer c ()) -> Producer c r
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Producer<RT, OUT_B, A> ForEach<RT, OUT_A, OUT_B, A>(
        this Producer<RT, OUT_A, A> p, 
        Func<OUT_A, Producer<RT, OUT_B, Unit>> body)
        where RT : HasIO<RT, Error> =>
        p.For(body).ToProducer();

    /// <summary>
    /// `p.ForEach(body)` loops over `Producer p` replacing each `yield` with `body`
    /// 
    ///     Producer b r -> (b -> Effect ()) -> Effect r
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Effect<RT, A> ForEach<RT, OUT, A>(
        this Producer<RT, OUT, A> p, 
        Func<OUT, Effect<RT, Unit>> fb)
        where RT : HasIO<RT, Error> =>
        p.For(fb).ToEffect();

    /// <summary>
    /// `p.ForEach(body)` loops over `Pipe p` replacing each `yield` with `body`
    /// 
    ///     Pipe x b r -> (b -> Consumer x ()) -> Consumer x r
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Consumer<RT, IN, A> ForEach<RT, IN, OUT, A>(
        this Pipe<RT, IN, OUT, A> p0, 
        Func<OUT, Consumer<RT, IN, Unit>> fb)
        where RT : HasIO<RT, Error> =>
        p0.For(fb).ToConsumer();

    /// <summary>
    /// `p.ForEach(body)` loops over `Pipe p` replacing each `yield` with `body`
    /// 
    ///     Pipe x b r -> (b -> Pipe x c ()) -> Pipe x c r
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Pipe<RT, IN, OUT, R> ForEach<RT, IN, B, OUT, R>(
        this Pipe<RT, IN, B, R> p0, 
        Func<B, Pipe<RT, IN, OUT, Unit>> fb)
        where RT : HasIO<RT, Error> =>
        p0.For(fb).ToPipe();

    /// <summary>
    /// `compose(draw, p)` loops over `p` replacing each `await` with `draw`
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Proxy<RT, UOut, UIn, DIn, DOut, B> compose<RT, UOut, UIn, DIn, DOut, A, B>(
        Proxy<RT, UOut, UIn, DIn, DOut, A> p1, 
        Proxy<RT, Unit, A, DIn, DOut, B> p2)
        where RT : HasIO<RT, Error> =>
        compose(_ => p1, p2);

    /// <summary>
    /// `compose(draw, p)` loops over `p` replacing each `await` with `draw`
    /// 
    ///     Effect b -> Consumer b c -> Effect c
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Effect<RT, A> compose<RT, OUT, A>(
        Effect<RT, OUT> p1, 
        Consumer<RT, OUT, A> p2)
        where RT : HasIO<RT, Error> =>
        compose(_ => p1, p2).ToEffect();

    /// <summary>
    /// `compose(draw, p)` loops over `p` replacing each `await` with `draw`
    /// 
    ///     Consumer a b -> Consumer b c -> Consumer a c
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Consumer<RT, A, C> compose<RT, A, B, C>(
        Consumer<RT, A, B> p1, 
        Consumer<RT, B, C> p2) 
        where RT : HasIO<RT, Error> =>
        compose(_ => p1, p2).ToConsumer();

    /// <summary>
    /// `compose(draw, p)` loops over `p` replacing each `await` with `draw`
    /// 
    ///     Producer y b -> Pipe b y m c -> Producer y c
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Producer<RT, OUT, C> compose<RT, OUT, IN, C>(
        Producer<RT, OUT, IN> p1, 
        Pipe<RT, IN, OUT, C> p2) 
        where RT : HasIO<RT, Error> =>
        compose(_ => p1, p2).ToProducer();

    /// <summary>
    /// `compose(draw, p)` loops over `p` replacing each `await` with `draw`
    /// 
    ///     Pipe a y b -> Pipe b y c -> Pipe a y c
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Pipe<RT, A, Y, C> compose<RT, Y, A, B, C>(
        Pipe<RT, A, Y, B> p1,
        Pipe<RT, B, Y, C> p2) 
        where RT : HasIO<RT, Error> =>
        compose(_ => p1, p2).ToPipe();

    // fixAwaitDual
    [Pure, MethodImpl(mops)]
    public static Proxy<RT, A1, A, Y1, Y, C> compose<RT, A1, A, Y1, Y, B, C>(
        Proxy<RT, Unit, B, Y1, Y, C> p2,
        Proxy<RT, A1, A, Y1, Y, B> p1) 
        where RT : HasIO<RT, Error> =>
        compose(p1, p2);

    /// <summary>
    /// Replaces each `request` or `respond` in `p0` with `fb1`.
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Proxy<RT, A1, A, Y1, Y, C> compose<RT, A1, A, B1, B, Y1, Y, C>(
        Func<B1, Proxy<RT, A1, A, Y1, Y, B>> fb1,
        Proxy<RT, B1, B, Y1, Y, C> p0) 
        where RT : HasIO<RT, Error> =>
        p0.ReplaceRequest(fb1);

    /// <summary>
    /// `compose(p, f)` pairs each `respond` in `p` with a `request` in `f`.
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Proxy<RT, A1, A, C1, C, R> compose<RT, A1, A, B1, B, C1, C, R>(
        Proxy<RT, A1, A, B1, B, R> p,
        Func<B, Proxy<RT, B1, B, C1, C, R>> fb)
        where RT : HasIO<RT, Error> =>
        p.PairEachRespondWithRequest(fb);

    /// <summary>
    /// `compose(f, p)` pairs each `request` in `p` with a `respond` in `f`
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Proxy<RT, A1, A, C1, C, R> compose<RT, A1, A, B1, B, C1, C, R>(
        Func<B1, Proxy<RT, A1, A, B1, B, R>> fb1,
        Proxy<RT, B1, B, C1, C, R> p)
        where RT : HasIO<RT, Error> =>
        p.PairEachRequestWithRespond(fb1);

    /// <summary>
    /// Pipe composition
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Proxy<RT, A1, A, C1, C, R> compose<RT, A1, A, B, C1, C, R>(
        Proxy<RT, A1, A, Unit, B, R> p1,
        Proxy<RT, Unit, B, C1, C, R> p2)
        where RT : HasIO<RT, Error> =>
        compose(_ => p1, p2);

    /// <summary>
    /// Pipe composition
    ///
    ///     Producer b r -> Consumer b r -> Effect m r
    /// 
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Effect<RT, R> compose<RT, B, R>(
        Producer<RT, B, R> p1,
        Consumer<RT, B, R> p2)
        where RT : HasIO<RT, Error> =>
        compose(p1.ToProxy(), p2).ToEffect();

    /// <summary>
    /// Pipe composition
    ///
    ///     Producer b r -> Pipe b c r -> Producer c r
    /// 
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Producer<RT, C, R> compose<RT, B, C, R>(
        Producer<RT, B, R> p1,
        Pipe<RT, B, C, R> p2)
        where RT : HasIO<RT, Error> =>
        compose(p1.ToProxy(), p2).ToProducer();

    /// <summary>
    /// Pipe composition
    ///
    ///     Pipe a b r -> Consumer b r -> Consumer a r
    /// 
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Consumer<RT, A, R> compose<RT, A, B, R>(
        Pipe<RT, A, B, R> p1,
        Consumer<RT, B, R> p2)
        where RT : HasIO<RT, Error> =>
        compose(p1.ToProxy(), p2).ToConsumer();

    /// <summary>
    /// Pipe composition
    ///
    ///     Pipe a b r -> Pipe b c r -> Pipe a c r
    /// 
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Pipe<RT, A, C, R> compose<RT, A, B, C, R>(
        Pipe<RT, A, B, R> p1,
        Pipe<RT, B, C, R> p2)
        where RT : HasIO<RT, Error> =>
        compose(p1.ToProxy(), p2).ToPipe();

    /// <summary>
    /// Compose two unfolds, creating a new unfold
    /// </summary>
    /// <remarks>
    /// This is the composition operator of the respond category.
    /// </remarks>
    [Pure, MethodImpl(mops)]
    public static Func<A, Proxy<RT, X1, X, C1, C, A1>> compose<RT, X1, X, A1, A, B1, B, C1, C>(
        Func<A, Proxy<RT, X1, X, B1, B, A1>> fa,
        Func<B, Proxy<RT, X1, X, C1, C, B1>> fb) 
        where RT : HasIO<RT, Error> =>
        a => compose(fa(a), fb);

    /// <summary>
    /// Compose two unfolds, creating a new unfold
    /// </summary>
    /// <remarks>
    /// This is the composition operator of the respond category.
    /// </remarks>
    [Pure, MethodImpl(mops)]
    public static Func<A, Proxy<RT, X1, X, C1, C, A1>> Then<RT, X1, X, A1, A, B1, B, C1, C>(
        this Func<A, Proxy<RT, X1, X, B1, B, A1>> fa,
        Func<B, Proxy<RT, X1, X, C1, C, B1>> fb) 
        where RT : HasIO<RT, Error> =>
        a => compose(fa(a), fb);

    /// <summary>
    /// `compose(p, f)` replaces each `respond` in `p` with `f`.
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Proxy<RT, X1, X, C1, C, A1> compose<RT, X1, X, A1, B1, C1, C, B>(
        Proxy<RT, X1, X, B1, B, A1> p0,
        Func<B, Proxy<RT, X1, X, C1, C, B1>> fb) 
        where RT : HasIO<RT, Error> =>
        p0.ReplaceRespond(fb);

    /// <summary>
    /// `compose(p, f)` replaces each `respond` in `p` with `f`.
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Proxy<RT, X1, X, C1, C, A1> Then<RT, X1, X, A1, B1, C1, C, B>(
        this Proxy<RT, X1, X, B1, B, A1> p0,
        Func<B, Proxy<RT, X1, X, C1, C, B1>> fb) 
        where RT : HasIO<RT, Error> =>
        compose(p0, fb);


    /// <summary>
    ///  Compose two folds, creating a new fold
    /// 
    ///     (f | g) x = f | g x
    /// 
    ///     | is the composition operator of the request category.
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Func<C1, Proxy<RT, A1, A, Y1, Y, C>> compose<RT, A1, A, B1, B, Y1, Y, C1, C>(
        Func<B1, Proxy<RT, A1, A, Y1, Y, B>> fb1,
        Func<C1, Proxy<RT, B1, B, Y1, Y, C>> fc1) 
        where RT : HasIO<RT, Error> =>
        c1 => compose(fb1, fc1(c1));

    /// <summary>
    /// 
    ///     observe(lift (Pure(r))) = observe(Pure(r))
    ///     observe(lift (m.Bind(f))) = observe(lift(m.Bind(x => lift(f(x)))))
    /// 
    /// This correctness comes at a small cost to performance, so use this function sparingly.
    /// This function is a convenience for low-level pipes implementers.  You do not need to
    /// use observe if you stick to the safe API.        
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Proxy<RT, A1, A, B1, B, R> observe<RT, A1, A, B1, B, R>(
        Proxy<RT, A1, A, B1, B, R> p0)
        where RT : HasIO<RT, Error> =>
        p0.Observe();

    /// <summary>
    /// `Absurd` function
    /// </summary>
    /// <param name="value">`Void` is supposed to represent `void`, nothing can be constructed from `void` and
    /// so this method just throws `ApplicationException("closed")`</param>
    [Pure, MethodImpl(mops)]
    public static A closed<A>(Void value) =>
        throw new ApplicationException("closed");

    /// <summary>
    /// Applicative apply
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Proxy<RT, A1, A, B1, B, S> apply<RT, A1, A, B1, B, R, S>(
        Proxy<RT, A1, A, B1, B, Func<R, S>> pf,
        Proxy<RT, A1, A, B1, B, R> px) where RT : HasIO<RT, Error>
    {
        return Go(pf);

        Proxy<RT, A1, A, B1, B, S> Go(Proxy<RT, A1, A, B1, B, Func<R, S>> p) =>
            p.ToProxy() switch
            {
                Request<RT, A1, A, B1, B, Func<R, S>> (var a1, var fa) => new Request<RT, A1, A, B1, B, S>(a1, a => Go(fa(a))),
                Respond<RT, A1, A, B1, B, Func<R, S>> (var b, var fb1) => new Respond<RT, A1, A, B1, B, S>(b, b1 => Go(fb1(b1))),
                M<RT, A1, A, B1, B, Func<R, S>> (var m)                => new M<RT, A1, A, B1, B, S>(m.MapRight(Go).Tail()),
                Pure<RT, A1, A, B1, B, Func<R, S>> (var f)             => px.Map(f),
                _                                                      => throw new NotSupportedException()
            };
    }

    /// <summary>
    /// Applicative apply
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Proxy<RT, A1, A, B1, B, S> Apply<RT, A1, A, B1, B, R, S>(this Proxy<RT, A1, A, B1, B, Func<R, S>> pf, Proxy<RT, A1, A, B1, B, R> px) 
        where RT : HasIO<RT, Error> =>
        apply(pf, px);

    /// <summary>
    /// Applicative action
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Proxy<RT, A1, A, B1, B, S> Action<RT, A1, A, B1, B, R, S>(this Proxy<RT, A1, A, B1, B, R> l, Proxy<RT, A1, A, B1, B, S> r) 
        where RT : HasIO<RT, Error> =>
        l.Action(r);

    /// <summary>
    /// Monad return / pure
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Proxy<RT, A1, A, B1, B, R> Pure<RT, A1, A, B1, B, R>(R value) 
        where RT : HasIO<RT, Error> =>
        new Pure<RT, A1, A, B1, B, R>(value);

    /// <summary>
    /// Creates a non-yielding producer that returns the result of the effects
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Transducer<RT, Sum<Error, (A, B)>> collect<RT, A, B>(Effect<RT, A> ma, Effect<RT, B> mb) 
        where RT : HasIO<RT, Error> =>
        ma.RunEffect().Zip(mb.RunEffect());

    /// <summary>
    /// Creates a non-yielding producer that returns the result of the effects
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Transducer<RT, Sum<Error, (A, B, C)>> collect<RT, A, B, C>(Effect<RT, A> ma, Effect<RT, B> mb, Effect<RT, C> mc) 
        where RT : HasIO<RT, Error> =>
        ma.RunEffect().Zip(mb.RunEffect(), mc.RunEffect());

    /// <summary>
    /// Creates a non-yielding producer that returns the result of the effects
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Producer<RT, (A, B), Unit> yield<RT, A, B>(Effect<RT, A> ma, Effect<RT, B> mb) where RT : HasIO<RT, Error> =>
        from r in collect(ma, mb).ToEff()
        from _ in yield(r)
        select unit;

    /// <summary>
    /// Creates a non-yielding producer that returns the result of the effects
    /// </summary>
    [Pure, MethodImpl(mops)]
    public static Producer<RT, (A, B, C), Unit> yield<RT, A, B, C>(Effect<RT, A> ma, Effect<RT, B> mb, Effect<RT, C> mc) where RT : HasIO<RT, Error> =>
        from r in collect(ma, mb, mc).ToEff()
        from _ in yield(r)
        select unit;

    /// <summary>
    /// Only forwards values that satisfy the predicate.
    /// </summary>
    public static Pipe<A, A, Unit> filter<A>(Func<A, bool> f) =>
        from x in awaiting<A>()
        from r in f(x) ? yield(x) : Prelude.Pure(unit)
        select r;

    /// <summary>
    /// Map the output of the pipe (not the bound value as is usual with Map)
    /// </summary>
    public static Pipe<A, B, Unit> map<A, B>(Func<A, B> f) =>
        from x in awaiting<A>()
        from r in yield(f(x))
        select r;

    /// <summary>
    /// Folds values coming down-stream, when the predicate returns false the folded value is yielded 
    /// </summary>
    /// <param name="Initial">Initial state</param>
    /// <param name="Fold">Fold operation</param>
    /// <param name="WhileState">Predicate</param>
    /// <returns>A pipe that folds</returns>
    [Pure, MethodImpl(mops)]
    public static Pipe<IN, OUT, Unit> foldWhile<IN, OUT>(OUT Initial, Func<OUT, IN, OUT> Fold, Func<OUT, bool> State) => 
        foldUntil(Initial, Fold, x => !State(x));
 
    /// <summary>
    /// Folds values coming down-stream, when the predicate returns true the folded value is yielded 
    /// </summary>
    /// <param name="Initial">Initial state</param>
    /// <param name="Fold">Fold operation</param>
    /// <param name="UntilState">Predicate</param>
    /// <returns>A pipe that folds</returns>
    public static Pipe<IN, OUT, Unit> foldUntil<IN, OUT>(OUT Initial, Func<OUT, IN, OUT> Fold, Func<OUT, bool> State)
    {
        var state = Initial;
        return awaiting<IN>()
           .Bind(x =>
                 {
                     state = Fold(state, x);
                     if (State(state))
                     {
                         var nstate = state;
                         state = Initial;
                         return yield(nstate);
                     }
                     else
                     {
                         return Prelude.Pure(unit);
                     }
                 });
    }        
        
    /// <summary>
    /// Folds values coming down-stream, when the predicate returns false the folded value is yielded 
    /// </summary>
    /// <param name="Initial">Initial state</param>
    /// <param name="Fold">Fold operation</param>
    /// <param name="WhileValue">Predicate</param>
    /// <returns>A pipe that folds</returns>
    [Pure, MethodImpl(mops)]
    public static Pipe<IN, OUT, Unit> foldWhile<IN, OUT>(OUT Initial, Func<OUT, IN, OUT> Fold, Func<IN, bool> Value) => 
        foldUntil(Initial, Fold, x => !Value(x));
 
    /// <summary>
    /// Folds values coming down-stream, when the predicate returns true the folded value is yielded 
    /// </summary>
    /// <param name="Initial">Initial state</param>
    /// <param name="Fold">Fold operation</param>
    /// <param name="UntilValue">Predicate</param>
    /// <returns>A pipe that folds</returns>
    public static Pipe<IN, OUT, Unit> foldUntil<IN, OUT>(OUT Initial, Func<OUT, IN, OUT> Fold, Func<IN, bool> Value)
    {
        var state = Initial;
        return awaiting<IN>()
           .Bind(x =>
                 {
                     if (Value(x))
                     {
                         var nstate = state;
                         state = Initial;
                         return yield(nstate);
                     }
                     else
                     {
                         state = Fold(state, x);
                         return Prelude.Pure(unit);
                     }
                 });
    }
}
