//
//  AgentNewsEntryViewModel.cs
//
//  Author:
//       Roman M. Yagodin <roman.yagodin@gmail.com>
//
//  Copyright (c) 2016 Roman M. Yagodin
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
using R7.Dnn.Extensions.ViewModels;
using R7.News.Agent.Components;
using R7.News.Models;
using R7.News.ViewModels;

namespace R7.News.Agent.ViewModels
{
    public class AgentNewsEntryViewModel: NewsEntryViewModelBase
    {
        public AgentNewsEntryViewModel (INewsEntry newsEntry, ViewModelContext<AgentSettings> context) :
            base (newsEntry, context)
        {
        }

        protected AgentSettings Settings
        {
            get { return ((ViewModelContext<AgentSettings>) Context).Settings; }
        }

        public new bool HasImage
        {
            get { return !Settings.HideImages && NewsEntry.GetImage () != null; }
        }

        public string ImageUrl
        {
            get { return NewsEntry.GetImageUrl (width: Settings.ThumbnailWidth); }
        }

        public string GroupImageUrl
        {
            get { return NewsEntry.GetImageUrl (width: Settings.GroupThumbnailWidth); }
        }

        public string FirstColumnContainerCssClass
        {
            get { return HasImage ? "col-md-6" : "hidden"; }
        }

        public string SecondColumnContainerCssClass
        {
            get { return HasImage ? "col-md-6" : "col-md-12"; }
        }
    }
}

