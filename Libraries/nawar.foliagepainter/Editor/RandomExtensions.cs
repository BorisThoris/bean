using System;
using System.Collections.Generic;

namespace Foliage;

public static class RandomExtensions
{
	public static T? FromList<T>( this Random random, IReadOnlyList<T> list )
	{
		if ( list.Count == 0 )
		{
			return default;
		}

		var index = random.Next( list.Count );
		return list[index];
	}
}
