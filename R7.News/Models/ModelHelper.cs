//
//  ModelHelper.cs
//
//  Author:
//       Roman M. Yagodin <roman.yagodin@gmail.com>
//
//  Copyright (c) 2016-2017 Roman M. Yagodin
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Affero General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Affero General Public License for more details.
//
//  You should have received a copy of the GNU Affero General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using DotNetNuke.Entities.Content.Taxonomy;

namespace R7.News.Models
{
    public static class ModelHelper
    {
        public static bool IsPublished (DateTime now, DateTime? startDate, DateTime? endDate)
        {
            return (startDate == null || now >= startDate) && (endDate == null || now < endDate);
        }

        public static bool HasBeenExpired (DateTime now, DateTime? startDate, DateTime? endDate)
        {
            return (endDate != null || now >= endDate);
        }

        public static DateTime PublishedOnDate (DateTime? startDate, DateTime createdOnDate)
        {
            return (startDate != null) ? startDate.Value : createdOnDate;
        }

        public static bool IsVisible (int thematicWeight,
                                      int structuralWeight, 
                                      int minThematicWeight,
                                      int maxThematicWeight,
                                      int minStructuralWeight,
                                      int maxStructuralWeight)
        {
            return IsThematicVisible (thematicWeight, minThematicWeight, maxThematicWeight)
            || IsStructuralVisible (structuralWeight, minStructuralWeight, maxStructuralWeight);
        }

        public static bool IsThematicVisible (int thematicWeight, int minThematicWeight, int maxThematicWeight)
        {
            return thematicWeight >= minThematicWeight && thematicWeight <= maxThematicWeight;
        }

        public static bool IsStructuralVisible (int structuralWeight, int minStructuralWeight, int maxStructuralWeight)
        {
            return structuralWeight >= minStructuralWeight && structuralWeight <= maxStructuralWeight;
        }

        public static bool IsTermsOverlaps (IEnumerable<Term> terms1, IEnumerable<Term> terms2)
        {
            var terms = terms1.Join (terms2, t1 => t1.TermId, t2 => t2.TermId, (t1, t2) => t1);

            return terms != null && terms.Any ();
        }
    }
}