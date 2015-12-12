using System;

public struct Vector2i
{
	public static readonly Vector2i zero = new Vector2i(0, 0);
	public static readonly Vector2i one = new Vector2i(1, 1);

	// VDF
	// ==========

	[VDFSerialize] VDFNode Serialize() { return x + " " + y; }
	[VDFDeserialize] void Deserialize(VDFNode node)
	{
		var parts = node.primitiveValue.ToString().Split(new[] {' '});
		x = int.Parse(parts[0]);
		y = int.Parse(parts[1]);
	}

	// operators and overrides
	// ==========

	/*int ShiftAndWrap(int value, int positions)
	{
		positions = positions & 0x1F;
		uint number = BitConverter.ToUInt32(BitConverter.GetBytes(value), 0); // save the existing bit pattern, but interpret it as an unsigned integer
		uint wrapped = number >> (32 - positions); // preserve the bits to be discarded
		return BitConverter.ToInt32(BitConverter.GetBytes((number << positions) | wrapped), 0); // shift and wrap the discarded bits
	}
	public override int GetHashCode()
	{
		return ((1789 + x.GetHashCode()) * 1789) + y.GetHashCode();
		//return ShiftAndWrap(x.GetHashCode(), 2) ^ y.GetHashCode();
	}*/
	//public override int GetHashCode() { return ToString().GetHashCode(); } // self/forum
	//public override int GetHashCode() { return (x << 16) ^ y; } // self/forum
	//public override int GetHashCode() { return x.GetHashCode() ^ y.GetHashCode()<<2 /*^ z.GetHashCode()>>2*/; } // Unity
	//public override int GetHashCode() { return (x * 397) ^ y; } // Resharper
	//public override int GetHashCode() { return (17*23 + x.GetHashCode())*23 + y.GetHashCode(); } // book/forum
	public override int GetHashCode() { return (17*23 + x)*23 + y; }
	public override bool Equals(object other)
	{
		if (!(other is Vector2i))
			return false;
		return this == (Vector2i)other;
	}
	public override string ToString() { return x + " " + y; }

	public static bool operator ==(Vector2i s, Vector2i b) { return s.x == b.x && s.y == b.y; }
	public static bool operator !=(Vector2i s, Vector2i b) { return !(s.x == b.x && s.y == b.y); }
	public static Vector2i operator -(Vector2i s, Vector2i b) { return new Vector2i(s.x - b.x, s.y - b.y); }
	public static Vector2i operator -(Vector2i s) { return new Vector2i(-s.x, -s.y); }
	public static Vector2i operator +(Vector2i s, Vector2i b) { return new Vector2i(s.x + b.x, s.y + b.y); }
	public static Vector2i operator *(Vector2i s, int amount) { return new Vector2i(amount * s.x, amount * s.y); }
	public static Vector2i operator *(int amount, Vector2i s) { return new Vector2i(amount * s.x, amount * s.y); }
	public static Vector2i operator /(Vector2i s, int amount) { return new Vector2i(s.x / amount, s.y / amount); }

	// export
	/*public VVector2 ToVVector2() { return this; }
	public Vector2 ToVector2(bool fromYForwardToUp = true)
	{
		if (fromYForwardToUp)
			return new Vector2(x, 0);
		return new Vector2(x, y);
	}
	public Vector3i ToVector3i() { return new Vector3i(x, y, 0); }*/

	// general
	// ==========

	//public static Vector2i Null { get { return new Vector2i(double.NaN, double.NaN); } }

	//public Vector2i(Vector2 obj) : this(obj.x, obj.y) {}
	public Vector2i(double x, double y) : this((int)Math.Floor(x), (int)Math.Floor(y)) { }
	public Vector2i(int x, int y)
	{
		this.x = x;
		this.y = y;
	}

	public int x;
	public int y;

	public double Distance(Vector2i other) { return (other - this).magnitude; }
	public double DistanceSquared(Vector2i other) { return (other - this).magnitudeSquared; }

	public double magnitude { get { return Math.Sqrt((x * x) + (y * y)); } }
	public double magnitudeSquared { get { return (x * x) + (y * y); } }

	public Vector2i NewX(int val) { return new Vector2i(val, y); }
	public Vector2i NewY(int val) { return new Vector2i(x, val); }

	/*public Vector2i FloorToMultipleOf(int val) { return new Vector2i(x.FloorToMultipleOf(val), y.FloorToMultipleOf(val)); }
	public Vector2i RoundToMultipleOf(int val) { return new Vector2i(x.RoundToMultipleOf(val), y.RoundToMultipleOf(val)); }*/

	//public Vector2i AsPositive() { return new Vector2i(x >= 0 ? x : -x, y >= 0 ? y : -1); }
}