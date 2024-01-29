using System;
using System.Threading;
using LanguageExt.Common;
using LanguageExt.Effects.Traits;
using static LanguageExt.Prelude;

namespace LanguageExt.Effects;

public static class MinRTExtensions
{
    public static MinRT ToMin<RT>(this RT rt) where RT : HasIO<RT, Error> =>
        new (rt.SynchronizationContext, rt.CancellationTokenSource, rt.CancellationToken);
    
    public static MinRT<E> ToMin<RT, E>(this RT rt) where RT : HasIO<RT, E> =>
        new (rt.SynchronizationContext, rt.CancellationTokenSource, rt.CancellationToken);
}

/// <summary>
/// Minimal runtime for running the non-runtime based IO monads
/// </summary>
public readonly struct MinRT : 
    HasIO<MinRT, Error>
{
    /// <summary>
    /// Get the transducer that converts from a `HasIO` supporting runtime to a `MinRT`
    /// </summary>
    public static Transducer<RT, MinRT> convert<RT>() 
        where RT : HasIO<RT, Error> =>
        lift<RT, MinRT>(rt => rt.ToMin());
    
    public MinRT(
        SynchronizationContext? syncContext,
        CancellationTokenSource cancellationTokenSource,
        CancellationToken cancellationToken) =>
        (SynchronizationContext, CancellationTokenSource, CancellationToken) = 
            (syncContext, cancellationTokenSource, cancellationToken);

    public MinRT(
        SynchronizationContext? syncContext,
        CancellationTokenSource cancellationTokenSource) =>
        (SynchronizationContext, CancellationTokenSource, CancellationToken) = 
            (syncContext, cancellationTokenSource, cancellationTokenSource.Token);

    public MinRT()
    {
        CancellationTokenSource = new CancellationTokenSource();
        CancellationToken = CancellationTokenSource.Token;
        SynchronizationContext = SynchronizationContext.Current;
    }

    public MinRT LocalCancel =>
        new (SynchronizationContext, CancellationTokenSource);
    
    public SynchronizationContext? SynchronizationContext { get; }
    public CancellationToken CancellationToken { get; }
    public CancellationTokenSource CancellationTokenSource { get; }
    
    public static Error FromError(Error error) => 
        error;

    public MinRT WithSyncContext(SynchronizationContext? syncContext) =>
        new (syncContext, CancellationTokenSource);
}

/// <summary>
/// Minimal runtime for running the non-runtime based IO monads
/// </summary>
public readonly struct MinRT<E> : 
    HasIO<MinRT<E>, E>
{
    public static Func<Error, E> DefaultErrorMap =
        e =>
        {
            e.Throw();
            throw new NotImplementedException();
        };
    
    /// <summary>
    /// Get the transducer that converts from a `HasIO` supporting runtime to a `MinRT`
    /// </summary>
    public static Transducer<RT, MinRT<E>> convert<RT>() 
        where RT : HasIO<RT, E> =>
        lift<RT, MinRT<E>>(rt => rt.ToMin<RT, E>());
    
    public MinRT(
        SynchronizationContext? syncContext,
        CancellationTokenSource cancellationTokenSource,
        CancellationToken cancellationToken) =>
        (SynchronizationContext, CancellationTokenSource, CancellationToken) = 
        (syncContext, cancellationTokenSource, cancellationToken);

    public MinRT(
        SynchronizationContext? syncContext,
        CancellationTokenSource cancellationTokenSource) =>
        (SynchronizationContext, CancellationTokenSource, CancellationToken) = 
        (syncContext, cancellationTokenSource, cancellationTokenSource.Token);

    public MinRT()
    {
        CancellationTokenSource = new CancellationTokenSource();
        CancellationToken       = CancellationTokenSource.Token;
        SynchronizationContext  = SynchronizationContext.Current;
    }

    public MinRT<E> LocalCancel =>
        new (SynchronizationContext, CancellationTokenSource);

    public SynchronizationContext? SynchronizationContext { get; }
    public CancellationToken CancellationToken { get; }
    public CancellationTokenSource CancellationTokenSource { get; }
    
    public static E FromError(Error error) =>
        DefaultErrorMap(error);

    public MinRT<E> WithSyncContext(SynchronizationContext? syncContext) =>
        new (syncContext, CancellationTokenSource);
}
