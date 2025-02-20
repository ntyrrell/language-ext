﻿using System;
using LanguageExt.Traits;
using LanguageExt.Pipes;

namespace LanguageExt.Pipes;

public static partial class Proxy
{
    /// <summary>
    /// Functor map operation
    /// </summary>
    /// <remarks>
    /// Unwraps the value within the functor, passes it to the map function `f` provided, and
    /// then takes the mapped value and wraps it back up into a new functor.
    /// </remarks>
    /// <param name="ma">Functor to map</param>
    /// <param name="f">Mapping function</param>
    /// <returns>Mapped functor</returns>
    public static Proxy<UOut, UIn, DIn, DOut, M, B> map<UOut, UIn, DIn, DOut, M, A, B>(
        Func<A, B> f, 
        K<Proxy<UOut, UIn, DIn, DOut, M>, A> ma) 
        where M : Monad<M> =>
        Functor.map(f, ma).As();
    
    /// <summary>
    /// Applicative action: runs the first applicative, ignores the result, and returns the second applicative
    /// </summary>
    public static Proxy<UOut, UIn, DIn, DOut, M, B> action<UOut, UIn, DIn, DOut, M, A, B>(
        K<Proxy<UOut, UIn, DIn, DOut, M>, A> ma, 
        K<Proxy<UOut, UIn, DIn, DOut, M>, B> mb) 
        where M : Monad<M> =>
        Applicative.action(ma, mb).As();

    /// <summary>
    /// Applicative functor apply operation
    /// </summary>
    /// <remarks>
    /// Unwraps the value within the `ma` applicative-functor, passes it to the unwrapped function(s) within `mf`, and
    /// then takes the resulting value and wraps it back up into a new applicative-functor.
    /// </remarks>
    /// <param name="ma">Value(s) applicative functor</param>
    /// <param name="mf">Mapping function(s)</param>
    /// <returns>Mapped applicative functor</returns>
    public static Proxy<UOut, UIn, DIn, DOut, M, B> apply<UOut, UIn, DIn, DOut, M, A, B>(
        K<Proxy<UOut, UIn, DIn, DOut, M>, Func<A, B>> mf, 
        K<Proxy<UOut, UIn, DIn, DOut, M>, A> ma) 
        where M : Monad<M> =>
        Applicative.apply(mf, ma).As();
}    
