using System;
using System.Runtime.InteropServices;
using SimpleBase;

namespace RaidBattlesBot.Model
{
  [StructLayout(LayoutKind.Explicit)]
  public struct PollId
  {
    [FieldOffset(0)]
    private long Packed;
    
    [FieldOffset(0)]
    public int Id;
    [FieldOffset(sizeof(int))]
    public VoteEnum Format;

    public static bool TryRead(ReadOnlySpan<char> text, out PollId pollId)
    {
      try
      {
        return MemoryMarshal.TryRead(Base58.Flickr.Decode(text), out pollId);
      }
      catch (Exception)
      {
        pollId = default;
        return false;
      }
    }

    public override string ToString()
    {
      Span<byte> packedArray = new byte[sizeof(long)];
      MemoryMarshal.Write(packedArray, ref Packed);
      return Base58.Flickr.Encode(packedArray);
    }
  }
}