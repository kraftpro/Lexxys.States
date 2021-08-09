using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace State.Test1
{
	class TextUtility
	{

		public unsafe static ulong NarrowUtf16ToAscii(char* pUtf16Buffer, byte* pAsciiBuffer, ulong elementCount)
		{
			ulong num = 0uL;
			uint num2 = 0u;
			uint num3 = 0u;
			ulong num4 = 0uL;
			uint num5;
			if (Sse2.IsSupported)
			{
				if (elementCount >= (uint)(2 * Unsafe.SizeOf<Vector128<byte>>()))
				{
					if (IntPtr.Size >= 8)
					{
						num4 = Unsafe.ReadUnaligned<ulong>(pUtf16Buffer);
						if (AllCharsInUInt64AreAscii(num4))
						{
							goto IL_005b;
						}
					}
					else
					{
						num2 = Unsafe.ReadUnaligned<uint>(pUtf16Buffer);
						num3 = Unsafe.ReadUnaligned<uint>(pUtf16Buffer + 2);
						if (AllCharsInUInt32AreAscii(num2 | num3))
						{
							goto IL_005b;
						}
					}
					goto IL_0206;
				}
			}
			else if (Vector.IsHardwareAccelerated)
			{
				num5 = (uint)Unsafe.SizeOf<Vector<byte>>();
				if (elementCount >= 2 * num5)
				{
					if (IntPtr.Size >= 8)
					{
						num4 = Unsafe.ReadUnaligned<ulong>(pUtf16Buffer);
						if (AllCharsInUInt64AreAscii(num4))
						{
							goto IL_00c1;
						}
					}
					else
					{
						num2 = Unsafe.ReadUnaligned<uint>(pUtf16Buffer);
						num3 = Unsafe.ReadUnaligned<uint>(pUtf16Buffer + 2);
						if (AllCharsInUInt32AreAscii(num2 | num3))
						{
							goto IL_00c1;
						}
					}
					goto IL_0206;
				}
			}
			goto IL_012b;
		IL_012b:
			ulong num6 = elementCount - num;
			if (num6 < 4)
			{
				goto IL_01b5;
			}
			ulong num7 = num + num6 - 4;
			while (true)
			{
				if (IntPtr.Size >= 8)
				{
					num4 = Unsafe.ReadUnaligned<ulong>(pUtf16Buffer + num);
					if (!AllCharsInUInt64AreAscii(num4))
					{
						break;
					}
					NarrowFourUtf16CharsToAsciiAndWriteToBuffer(ref pAsciiBuffer[num], num4);
				}
				else
				{
					num2 = Unsafe.ReadUnaligned<uint>(pUtf16Buffer + num);
					num3 = Unsafe.ReadUnaligned<uint>(pUtf16Buffer + num + 2);
					if (!AllCharsInUInt32AreAscii(num2 | num3))
					{
						break;
					}
					NarrowTwoUtf16CharsToAsciiAndWriteToBuffer(ref pAsciiBuffer[num], num2);
					NarrowTwoUtf16CharsToAsciiAndWriteToBuffer(ref pAsciiBuffer[num + 2], num3);
				}
				num += 4;
				if (num <= num7)
				{
					continue;
				}
				goto IL_01b5;
			}
			goto IL_0206;
		IL_01b5:
			if (((uint)(int)num6 & 2u) != 0)
			{
				num2 = Unsafe.ReadUnaligned<uint>(pUtf16Buffer + num);
				if (!AllCharsInUInt32AreAscii(num2))
				{
					goto IL_0264;
				}
				NarrowTwoUtf16CharsToAsciiAndWriteToBuffer(ref pAsciiBuffer[num], num2);
				num += 2;
			}
			if (((uint)(int)num6 & (true ? 1u : 0u)) != 0)
			{
				num2 = pUtf16Buffer[num];
				if (num2 <= 127)
				{
					pAsciiBuffer[num] = (byte)num2;
					num++;
				}
			}
			goto IL_0204;
		IL_00c1:
			var right = new Vector<ushort>(127);
			ulong num8 = elementCount - 2 * num5;
			do
			{
				Vector<ushort> vector = Unsafe.ReadUnaligned<Vector<ushort>>(pUtf16Buffer + num);
				Vector<ushort> vector2 = Unsafe.ReadUnaligned<Vector<ushort>>(pUtf16Buffer + num + Vector<ushort>.Count);
				if (Vector.GreaterThanAny(Vector.BitwiseOr(vector, vector2), right))
				{
					break;
				}
				var value = Vector.Narrow(vector, vector2);
				Unsafe.WriteUnaligned(pAsciiBuffer + num, value);
				num += num5;
			}
			while (num <= num8);
			goto IL_012b;
		IL_0204:
			return num;
		IL_005b:
			num = NarrowUtf16ToAscii_Sse2(pUtf16Buffer, pAsciiBuffer, elementCount);
			goto IL_012b;
		IL_0264:
			if (FirstCharInUInt32IsAscii(num2))
			{
				if (!BitConverter.IsLittleEndian)
				{
					num2 >>= 16;
				}
				pAsciiBuffer[num] = (byte)num2;
				num++;
			}
			goto IL_0204;
		IL_0206:
			if (IntPtr.Size >= 8)
			{
				num2 = (uint)((!BitConverter.IsLittleEndian) ? (num4 >> 32) : num4);
				if (AllCharsInUInt32AreAscii(num2))
				{
					NarrowTwoUtf16CharsToAsciiAndWriteToBuffer(ref pAsciiBuffer[num], num2);
					num2 = (uint)((!BitConverter.IsLittleEndian) ? num4 : (num4 >> 32));
					num += 2;
				}
			}
			else if (AllCharsInUInt32AreAscii(num2))
			{
				NarrowTwoUtf16CharsToAsciiAndWriteToBuffer(ref pAsciiBuffer[num], num2);
				num2 = num3;
				num += 2;
			}
			goto IL_0264;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool AllCharsInUInt32AreAscii(uint value)
		{
			return (value & 0xFF80FF80u) == 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool AllCharsInUInt64AreAscii(ulong value)
		{
			return (value & 0xFF80FF80FF80FF80uL) == 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void NarrowFourUtf16CharsToAsciiAndWriteToBuffer(ref byte outputBuffer, ulong value)
		{
			if (Bmi2.X64.IsSupported)
			{
				Unsafe.WriteUnaligned(ref outputBuffer, (uint)Bmi2.X64.ParallelBitExtract(value, 71777214294589695uL));
			}
			else if (BitConverter.IsLittleEndian)
			{
				outputBuffer = (byte)value;
				value >>= 16;
				Unsafe.Add(ref outputBuffer, 1) = (byte)value;
				value >>= 16;
				Unsafe.Add(ref outputBuffer, 2) = (byte)value;
				value >>= 16;
				Unsafe.Add(ref outputBuffer, 3) = (byte)value;
			}
			else
			{
				Unsafe.Add(ref outputBuffer, 3) = (byte)value;
				value >>= 16;
				Unsafe.Add(ref outputBuffer, 2) = (byte)value;
				value >>= 16;
				Unsafe.Add(ref outputBuffer, 1) = (byte)value;
				value >>= 16;
				outputBuffer = (byte)value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void NarrowTwoUtf16CharsToAsciiAndWriteToBuffer(ref byte outputBuffer, uint value)
		{
			if (BitConverter.IsLittleEndian)
			{
				outputBuffer = (byte)value;
				Unsafe.Add(ref outputBuffer, 1) = (byte)(value >> 16);
			}
			else
			{
				Unsafe.Add(ref outputBuffer, 1) = (byte)value;
				outputBuffer = (byte)(value >> 16);
			}
		}

		private unsafe static ulong NarrowUtf16ToAscii_Sse2(char* pUtf16Buffer, byte* pAsciiBuffer, ulong elementCount)
		{
			uint num = (uint)Unsafe.SizeOf<Vector128<byte>>();
			ulong num2 = num - 1;
			var right = Vector128.Create((short)(-128));
			var right2 = Vector128.Create(short.MinValue);
			var right3 = Vector128.Create((short)(-32641));
			Vector128<short> vector = Sse2.LoadVector128((short*)pUtf16Buffer);
			if (Sse41.IsSupported)
			{
				if (!Sse41.TestZ(vector, right))
					return 0uL;
			}
			else if (Sse2.MoveMask(Sse2.CompareGreaterThan(Sse2.Xor(vector, right2), right3).AsByte()) != 0)
			{
				return 0uL;
			}
			Vector128<byte> vector2 = Sse2.PackUnsignedSaturate(vector, vector);
			Sse2.StoreScalar((ulong*)pAsciiBuffer, vector2.AsUInt64());
			ulong num3 = num / 2u;
			if (((uint)(int)pAsciiBuffer & (num / 2u)) == 0)
			{
				vector = Sse2.LoadVector128((short*)(pUtf16Buffer + num3));
				if (Sse41.IsSupported)
				{
					if (Sse41.TestZ(vector, right))
					{
						goto IL_00cd;
					}
				}
				else if (Sse2.MoveMask(Sse2.CompareGreaterThan(Sse2.Xor(vector, right2), right3).AsByte()) == 0)
				{
					goto IL_00cd;
				}
				goto IL_017f;
			IL_017f:
				return num3;
			}

		IL_00e9:
			num3 = num - ((ulong)pAsciiBuffer & num2);
			ulong num4 = elementCount - num;
			do
			{
				vector = Sse2.LoadVector128((short*)(pUtf16Buffer + num3));
				Vector128<short> right4 = Sse2.LoadVector128((short*)(pUtf16Buffer + num3 + num / 2u));
				Vector128<short> left = Sse2.Or(vector, right4);
				if (Sse41.IsSupported)
				{
					if (Sse41.TestZ(left, right))
					{
						goto IL_0158;
					}
				}
				else if (Sse2.MoveMask(Sse2.CompareGreaterThan(Sse2.Xor(left, right2), right3).AsByte()) == 0)
				{
					goto IL_0158;
				}
				if (Sse41.IsSupported)
				{
					if (!Sse41.TestZ(vector, right))
					{
						break;
					}
				}
				else if (Sse2.MoveMask(Sse2.CompareGreaterThan(Sse2.Xor(vector, right2), right3).AsByte()) != 0)
				{
					break;
				}
				vector2 = Sse2.PackUnsignedSaturate(vector, vector);
				Sse2.StoreScalar((ulong*)(pAsciiBuffer + num3), vector2.AsUInt64());
				num3 += num / 2u;
				break;
			IL_0158:
				vector2 = Sse2.PackUnsignedSaturate(vector, right4);
				Sse2.StoreAligned(pAsciiBuffer + num3, vector2);
				num3 += num;
			}
			while (num3 <= num4);
			return num3;
		IL_00cd:
			vector2 = Sse2.PackUnsignedSaturate(vector, vector);
			Sse2.StoreScalar((ulong*)(pAsciiBuffer + num3), vector2.AsUInt64());
			goto IL_00e9;
		}

		private static bool FirstCharInUInt32IsAscii(uint value)
		{
			return
				BitConverter.IsLittleEndian && (value & 0xFF80u) == 0 ||
				!BitConverter.IsLittleEndian &&(value & 0xFF800000u) == 0;
		}
	}
}
