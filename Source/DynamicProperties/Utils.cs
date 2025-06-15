using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Shabby.DynamicProperties;

public static class Utils
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsDestroyed(this UnityEngine.Object obj) => obj.m_CachedPtr == IntPtr.Zero;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsNullref(this UnityEngine.Object obj) => ReferenceEquals(obj, null);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool ApproxEqualsAbs(float a, float b, float eps) =>
		Math.Abs(b - a) <= eps;

	/// https://randomascii.wordpress.com/2012/02/25/comparing-floating-point-numbers-2012-edition/
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool ApproxEqualsRel(float a, float b,
		float absDiff = 1e-4f, float relDiff = float.Epsilon)
	{
		if (a == b) return true;

		var diff = Math.Abs(a - b);
		if (diff < absDiff) return true;

		a = Math.Abs(a);
		b = Math.Abs(b);
		var largest = b > a ? b : a;
		return diff <= largest * relDiff;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool ApproxEqualsRel(Vector4 a, Vector4 b,
		float absDiff = 1e-4f, float relDiff = float.Epsilon) =>
		ApproxEqualsRel(a.x, b.x, absDiff, relDiff) &&
		ApproxEqualsRel(a.y, b.y, absDiff, relDiff) &&
		ApproxEqualsRel(a.z, b.z, absDiff, relDiff) &&
		ApproxEqualsRel(a.w, b.w, absDiff, relDiff);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool ApproxEquals(Color a, Color b, float eps = 1e-2f) =>
		ApproxEqualsAbs(a.r, b.r, eps) && ApproxEqualsAbs(a.g, b.g, eps) &&
		ApproxEqualsAbs(a.b, b.b, eps) && ApproxEqualsAbs(a.a, b.a, eps);
}
