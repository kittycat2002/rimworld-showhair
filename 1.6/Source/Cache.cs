using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace ShowHair;

public static class Cache
{
	public static readonly Dictionary<int, HatStateParms> hatStateDictionary = [];
	public static readonly Dictionary<int, (Graphic_Multi?, Graphic_Multi?)> extraHairGraphicsDictionary = [];
}