using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AcrossReportDesigner.Engines
{
    public sealed class GroupState
    {
        public string Field { get; }
        public string? PrevValue { get; set; }

        public GroupState(string field)
        {
            Field = field;
            PrevValue = null;
        }
    }

    public sealed class GroupLayoutContext
    {
        public List<GroupState> GroupStates { get; }

        public GroupLayoutContext(IEnumerable<string> groupFields)
        {
            GroupStates = groupFields
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Select(f => new GroupState(f!))
                .ToList();
        }

        public void Reset()
        {
            foreach (var s in GroupStates)
                s.PrevValue = null;
        }

        /// <summary>
        /// 変化した最上位レベルを返す（なければ -1）
        /// </summary>
        public int DetectFirstChangedLevel(
            Dictionary<string, string> row)
        {

            for (int level = 0; level < GroupStates.Count; level++)
            {
                var state = GroupStates[level];

                row.TryGetValue(state.Field, out var current);

                if (state.PrevValue != current)
                {
                    state.PrevValue = current;
                    return level;
                }
            }

            return -1;
        }

        public int LevelCount => GroupStates.Count;
    }
}
