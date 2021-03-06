﻿using System;
using System.Collections;
using System.Linq;

using AirBreather.Core.Collections;

using Xunit;

namespace AirBreather.Core.Tests
{
    public sealed class BitListTests
    {
        [Fact]
        public void ThrowingForBadArgs()
        {
            BitList bl = new BitList { true, true, false, false };

            Assert.Throws<ArgumentOutOfRangeException>(() => bl[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => bl[4]);
            Assert.Throws<ArgumentOutOfRangeException>(() => bl[-1] = false);
            Assert.Throws<ArgumentOutOfRangeException>(() => bl[4] = false);
            Assert.Throws<ArgumentOutOfRangeException>(() => bl.Insert(-1, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => bl.Insert(5, true));
            Assert.Throws<ArgumentOutOfRangeException>(() => bl.RemoveAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => bl.RemoveAt(4));
            Assert.Throws<ArgumentNullException>(() => bl.CopyTo(null, 5));

            bool[] things = new bool[5];
            Assert.Throws<ArgumentOutOfRangeException>(() => bl.CopyTo(things, -1));
            Assert.Throws<ArgumentException>(() => bl.CopyTo(things, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => bl.CopyTo(things, 5));

            things = new bool[3];
            Assert.Throws<ArgumentException>(() => bl.CopyTo(things, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => bl.CopyTo(things, 3));
        }

        [Fact]
        public void AddAndRemoveShouldBeConsistent()
        {
            BitList bl = new BitList();

            bl.Add(false);
            bl.Add(false);

            Assert.Equal(false, bl[0]);
            Assert.Equal(2, bl.Count);

            Assert.False(bl.Remove(true));
            Assert.Equal(2, bl.Count);

            Assert.True(bl.Remove(false));

            Assert.Equal(1, bl.Count);
        }

        [Fact]
        public void InsertAndRemoveAtShouldBeConsistent()
        {
            BitList bl = new BitList { true, false, true };

            bl.Insert(1, false);

            Assert.Equal(4, bl.Count);

            bool[] expected = { true, false, false, true };
            Assert.Equal(expected, bl);

            bl.RemoveAt(2);

            Assert.Equal(3, bl.Count);
            expected = new[] { true, false, true };
            Assert.Equal(expected, bl);
        }

        [Fact]
        public void ShouldNotBeReadOnly()
        {
            BitList bl = new BitList();

            Assert.False(bl.IsReadOnly);
        }

        [Fact]
        public void Contains()
        {
            BitList bl = new BitList();

            Assert.False(bl.Contains(true));
            Assert.False(bl.Contains(false));

            bl.Add(true);

            Assert.Equal(1, bl.Count);
            Assert.True(bl.Contains(true));
            Assert.False(bl.Contains(false));

            bl.Clear();
            bl.TrimExcess();

            Assert.Equal(0, bl.Count);
            Assert.False(bl.Contains(true));
            Assert.False(bl.Contains(false));

            bl.Insert(0, false);
            Assert.Equal(1, bl.Count);
            Assert.False(bl.Contains(true));
            Assert.True(bl.Contains(false));

            bl[0] = true;
            Assert.Equal(1, bl.Count);
            Assert.True(bl.Contains(true));
            Assert.False(bl.Contains(false));

            bl[0] = false;
            Assert.Equal(1, bl.Count);
            Assert.False(bl.Contains(true));
            Assert.True(bl.Contains(false));
        }

        [Fact]
        public void CopyToShouldWork()
        {
            const int ByteCount = 1927;
            const int BoolCount = ByteCount * 8;
            const int ArrayOffset = 3871;

            byte[] someStuff = new byte[ByteCount];
            new System.Random().NextBytes(someStuff);

            BitList bl = new BitList();
            for (int i = 0; i < someStuff.Length; i++)
            {
                byte val = someStuff[i];
                byte mask = 1;
                for (int j = 0; j < 8; j++)
                {
                    bl.Add((val & mask) > 0);
                    mask <<= 1;
                }
            }

            bool[] vals = new bool[BoolCount + ArrayOffset];
            bl.CopyTo(vals, ArrayOffset);

            Assert.Equal(bl, vals.Skip(ArrayOffset));
        }

        [Fact]
        public void ToBitArrayShouldWork()
        {
            const int ByteCount = 1927;
            const int BoolCount = ByteCount * 8;
            const int ArrayOffset = 3871;

            byte[] someStuff = new byte[ByteCount];
            new System.Random().NextBytes(someStuff);

            BitList bl = new BitList();
            for (int i = 0; i < someStuff.Length; i++)
            {
                byte val = someStuff[i];
                byte mask = 1;
                for (int j = 0; j < 8; j++)
                {
                    bl.Add((val & mask) > 0);
                    mask <<= 1;
                }
            }

            BitArray ba = bl.ToBitArray();

            Assert.Equal(bl.Count, ba.Length);
            for (int i = 0; i < bl.Count; i++)
            {
                Assert.Equal(bl[i], ba[i]);
            }
        }
    }
}
