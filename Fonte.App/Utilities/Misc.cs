/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Utilities
{
    using System.Collections.Generic;
    using System.Linq;

    class Misc
    {
        public struct GuidelineRule
        {
            public Data.Guideline Guideline { get; }

            public GuidelineRule(Data.Guideline guideline)
            {
                Guideline = guideline;
            }
        }

        public static IEnumerable<Data.Guideline> GetAllGuidelines(Data.Layer layer)
        {
            return Enumerable.Concat(layer.Guidelines, GetMasterGuidelines(layer));
        }

        public static IEnumerable<Data.Guideline> GetMasterGuidelines(Data.Layer layer)
        {
            return layer.Master?.Guidelines ?? Enumerable.Empty<Data.Guideline>();
        }

        public static Data.Guideline GetSelectedGuideline(Data.Layer layer)
        {
            var selection = layer.Selection;
            if (selection.Count > 1)
            {
            }
            else if (selection.Count > 0)
            {
                return layer.Selection.First() as Data.Guideline;
            }
            else if (layer.Master is Data.Master master)
            {
                return master.Guidelines.Where(g => g.IsSelected).FirstOrDefault();
            }
            return null;
        }
    }
}