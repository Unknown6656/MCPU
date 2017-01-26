using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System;

using static System.Math;

// CREDIT GOES TO https://www.codeproject.com/Articles/6943/A-Generic-Reusable-Diff-Algorithm-in-C-II

namespace MCPU
{
    public interface IDiffList
    {
        int Count();
        IComparable GetByIndex(int index);
    }

    public class DiffEngine
    {
        private DiffStateList _stateList;
        private ArrayList _matchList;
        private DiffEngineLevel _level;
        private IDiffList _source;
        private IDiffList _dest;


        public DiffEngine()
        {
            _source = null;
            _dest = null;
            _matchList = null;
            _stateList = null;
            _level = DiffEngineLevel.FastImperfect;
        }

        private int GetSourceMatchLength(int destIndex, int sourceIndex, int maxLength)
        {
            int matchCount;

            for (matchCount = 0; matchCount < maxLength; matchCount++)
                if (_dest.GetByIndex(destIndex + matchCount).CompareTo(_source.GetByIndex(sourceIndex + matchCount)) != 0)
                    break;

            return matchCount;
        }

        private void GetLongestSourceMatch(DiffState curItem, int destIndex, int destEnd, int sourceStart, int sourceEnd)
        {
            int mdestlen = (destEnd - destIndex) + 1;
            int currlen = 0, mlen = 0;
            int curBestLength = 0;
            int curBestIndex = -1;

            for (int sourceIndex = sourceStart; sourceIndex <= sourceEnd; sourceIndex++)
            {
                if ((mlen = Min(mdestlen, (sourceEnd - sourceIndex) + 1)) <= curBestLength)
                    break;

                if ((currlen = GetSourceMatchLength(destIndex, sourceIndex, mlen)) > curBestLength)
                {
                    curBestIndex = sourceIndex;
                    curBestLength = currlen;
                }

                sourceIndex += curBestLength;
            }

            //DiffState cur = _stateList.GetByIndex(destIndex);
            if (curBestIndex == -1)
                curItem.SetNoMatch();
            else
                curItem.SetMatch(curBestIndex, curBestLength);
        }

        private void ProcessRange(int destStart, int destEnd, int sourceStart, int sourceEnd)
        {
            DiffState bitem = null, citem = null;
            int maxPossibleDestLength = 0;
            int curBestLength = -1;
            int curBestIndex = -1;

            for (int destIndex = destStart; destIndex <= destEnd; destIndex++)
            {
                maxPossibleDestLength = (destEnd - destIndex) + 1;

                if (maxPossibleDestLength <= curBestLength)
                    break;

                citem = _stateList.GetByIndex(destIndex);

                if (!citem.HasValidLength(sourceStart, sourceEnd, maxPossibleDestLength))
                    GetLongestSourceMatch(citem, destIndex, destEnd, sourceStart, sourceEnd);
                if (citem.Status == DiffStatus.Matched)
                    switch (_level)
                    {
                        case DiffEngineLevel.FastImperfect:
                            if (citem.Length > curBestLength)
                            {
                                //this is longest match so far
                                curBestIndex = destIndex;
                                curBestLength = citem.Length;
                                bitem = citem;
                            }
                            //Jump over the match 
                            destIndex += citem.Length - 1;
                            break;
                        case DiffEngineLevel.Medium:
                            if (citem.Length > curBestLength)
                            {
                                //this is longest match so far
                                curBestIndex = destIndex;
                                curBestLength = citem.Length;
                                bitem = citem;
                                //Jump over the match 
                                destIndex += citem.Length - 1;
                            }
                            break;
                        default:
                            if (citem.Length > curBestLength)
                            {
                                //this is longest match so far
                                curBestIndex = destIndex;
                                curBestLength = citem.Length;
                                bitem = citem;
                            }
                            break;
                    }
            }

            if (curBestIndex >= 0)
            {
                int sourceIndex = bitem.StartIndex;

                _matchList.Add(DiffResultSpan.CreateNoChange(curBestIndex, sourceIndex, curBestLength));

                if ((destStart < curBestIndex) && (sourceStart < sourceIndex))
                    ProcessRange(destStart, curBestIndex - 1, sourceStart, sourceIndex - 1);

                int upperDestStart = curBestIndex + curBestLength;
                int upperSourceStart = sourceIndex + curBestLength;

                if ((destEnd > upperDestStart) && (sourceEnd > upperSourceStart))
                    ProcessRange(upperDestStart, destEnd, upperSourceStart, sourceEnd);
            }
        }

        public double ProcessDiff(IDiffList source, IDiffList destination, DiffEngineLevel level)
        {
            _level = level;

            return ProcessDiff(source, destination);
        }

        public double ProcessDiff(IDiffList source, IDiffList destination)
        {
            DateTime dt = DateTime.Now;
            _source = source;
            _dest = destination;
            _matchList = new ArrayList();

            int dcount = _dest.Count();
            int scount = _source.Count();
            
            if ((dcount > 0) && (scount > 0))
            {
                _stateList = new DiffStateList(dcount);
                ProcessRange(0, dcount - 1, 0, scount - 1);
            }

            TimeSpan ts = DateTime.Now - dt;
            return ts.TotalSeconds;
        }

        private bool AddChanges(List<DiffResultSpan> report, int curDest, int nextDest, int curSource, int nextSource)
        {
            bool retval = false;
            int diffDest = nextDest - curDest;
            int diffSource = nextSource - curSource;
            int minDiff = 0;

            if (diffDest > 0)
            {
                if (diffSource > 0)
                {
                    minDiff = Min(diffDest, diffSource);
                    report.Add(DiffResultSpan.CreateReplace(curDest, curSource, minDiff));

                    if (diffDest > diffSource)
                    {
                        curDest += minDiff;
                        report.Add(DiffResultSpan.CreateAddDestination(curDest, diffDest - diffSource));
                    }
                    else if (diffSource > diffDest)
                    {
                        curSource += minDiff;
                        report.Add(DiffResultSpan.CreateDeleteSource(curSource, diffSource - diffDest));
                    }
                }
                else
                    report.Add(DiffResultSpan.CreateAddDestination(curDest, diffDest));

                retval = true;
            }
            else if (diffSource > 0)
            {
                report.Add(DiffResultSpan.CreateDeleteSource(curSource, diffSource));
                retval = true;
            }

            return retval;
        }

        public DiffResultSpan[] DiffReport
        {
            get
            {
                List<DiffResultSpan> retval = new List<DiffResultSpan>();
                int dcount = _dest.Count();
                int scount = _source.Count();

                if (dcount == 0)
                {
                    if (scount > 0)
                        retval.Add(DiffResultSpan.CreateDeleteSource(0, scount));

                    return retval.ToArray();
                }
                else
                {
                    if (scount == 0)
                    {
                        retval.Add(DiffResultSpan.CreateAddDestination(0, dcount));

                        return retval.ToArray();
                    }
                }

                _matchList.Sort();

                int curDest = 0;
                int curSource = 0;
                DiffResultSpan last = null;

                foreach (DiffResultSpan drs in _matchList)
                {
                    if ((!AddChanges(retval, curDest, drs.DestinationIndex, curSource, drs.SourceIndex)) && (last != null))
                        last.AddLength(drs.Length);
                    else
                        retval.Add(drs);

                    curDest = drs.DestinationIndex + drs.Length;
                    curSource = drs.SourceIndex + drs.Length;
                    last = drs;
                }

                AddChanges(retval, curDest, dcount, curSource, scount);

                return retval.ToArray();
            }
        }
    }

    public sealed class CharacterDiffList
        : IDiffList
    {
        private string[] _data;
        

        public CharacterDiffList(string text) => _data = text.Split('\n');

        public int Count() => _data.Length;

        public string GetByIndex(int index) => _data[index];

        IComparable IDiffList.GetByIndex(int index) => GetByIndex(index);

        public static implicit operator CharacterDiffList(string val) => new CharacterDiffList(val);
    }

    internal class DiffState
    {
        private const int BAD_INDEX = -1;
        private int _startIndex;
        private int _length;

        public int StartIndex => _startIndex;
        public int EndIndex => ((_startIndex + _length) - 1);

        public int Length
        {
            get
            {
                int len;

                if (_length > 0)
                    len = _length;
                else
                {
                    if (_length == 0)
                        len = 1;
                    else
                        len = 0;
                }

                return len;
            }
        }

        public DiffStatus Status
        {
            get
            {
                if (_length > 0)
                    return DiffStatus.Matched;
                else if (_length == -1)
                    return DiffStatus.NoMatch;
                else
                {
                    Debug.Assert(_length == -2, "Invalid status: _length < -2");

                    return DiffStatus.Unknown;
                }
            }
        }


        public DiffState() =>
            SetToUnkown();

        protected void SetToUnkown()
        {
            _startIndex = BAD_INDEX;
            _length = (int)DiffStatus.Unknown;
        }

        public void SetNoMatch()
        {
            _startIndex = BAD_INDEX;
            _length = (int)DiffStatus.NoMatch;
        }

        public void SetMatch(int start, int length)
        {
            Debug.Assert(length > 0, "Length must be greater than zero");
            Debug.Assert(start >= 0, "Start must be greater than or equal to zero");

            _startIndex = start;
            _length = length;
        }

        public bool HasValidLength(int newStart, int newEnd, int maxPossibleDestLength)
        {
            if (_length > 0)
                if ((maxPossibleDestLength < _length) || ((_startIndex < newStart) || (EndIndex > newEnd)))
                    SetToUnkown();

            return _length != (int)DiffStatus.Unknown;
        }
    }

    internal class DiffStateList
    {
#if USE_HASH_TABLE
		private Hashtable _table;
#else
        private DiffState[] _array;
#endif

        public DiffStateList(int destCount) =>
#if USE_HASH_TABLE
			_table = new Hashtable(Max(9, destCount / 10));
#else
            _array = new DiffState[destCount];
#endif

        public DiffState GetByIndex(int index)
        {
#if USE_HASH_TABLE
			DiffState retval = (DiffState)_table[index];

			if (retval == null)
			{
				retval = new DiffState();
				_table.Add(index, retval);
			}
#else
            DiffState retval = _array[index];

            if (retval == null)
            {
                retval = new DiffState();
                _array[index] = retval;
            }
#endif
            return retval;
        }
    }

    public sealed class DiffResultSpan
        : IComparable
    {
        private const int BAD_INDEX = -1;

        public DiffResultSpanStatus Status { get; }
        public int Length { get; private set; }
        public int DestinationIndex { get; }
        public int SourceIndex { get; }
       

        internal DiffResultSpan(DiffResultSpanStatus status, int destIndex, int sourceIndex, int length)
        {
            Status = status;
            DestinationIndex = destIndex;
            SourceIndex = sourceIndex;
            Length = length;
        }

        public static DiffResultSpan CreateNoChange(int destIndex, int sourceIndex, int length) => new DiffResultSpan(DiffResultSpanStatus.NoChange, destIndex, sourceIndex, length);

        public static DiffResultSpan CreateReplace(int destIndex, int sourceIndex, int length) => new DiffResultSpan(DiffResultSpanStatus.Replace, destIndex, sourceIndex, length);

        public static DiffResultSpan CreateDeleteSource(int sourceIndex, int length) => new DiffResultSpan(DiffResultSpanStatus.DeleteSource, BAD_INDEX, sourceIndex, length);

        public static DiffResultSpan CreateAddDestination(int destIndex, int length) => new DiffResultSpan(DiffResultSpanStatus.AddDestination, destIndex, BAD_INDEX, length);

        public void AddLength(int i) => Length += i;

        public override string ToString() => $"{Status} (Dest: {DestinationIndex},Source: {SourceIndex}) {Length}";

        public int CompareTo(object obj) => DestinationIndex.CompareTo(((DiffResultSpan)obj).DestinationIndex);
    }

    public enum DiffResultSpanStatus
        : byte
    {
        NoChange,
        Replace,
        DeleteSource,
        AddDestination
    }

    public enum DiffEngineLevel
        : byte
    {
        FastImperfect,
        Medium,
        SlowPerfect
    }

    internal enum DiffStatus
        : sbyte
    {
        Matched = 1,
        NoMatch = -1,
        Unknown = -2
    }
}
