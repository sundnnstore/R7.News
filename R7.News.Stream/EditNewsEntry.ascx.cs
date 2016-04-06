﻿//
//  EditNewsEntry.ascx.cs
//
//  Author:
//       Roman M. Yagodin <roman.yagodin@gmail.com>
//
//  Copyright (c) 2016 Roman M. Yagodin
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Content.Taxonomy;
using DotNetNuke.Entities.Modules;
using DotNetNuke.R7;
using DotNetNuke.R7.Entities.Modules;
using DotNetNuke.R7.ViewModels;
using DotNetNuke.Services.FileSystem;
using R7.News.Components;
using R7.News.ControlExtensions;
using R7.News.Data;
using R7.News.Models;
using R7.News.Stream.Components;
using R7.News.Stream.ViewModels;

namespace R7.News.Stream
{
    public enum EditNewsEntryTab { Common, Citation, Advanced };

    public partial class EditNewsEntry : EditPortalModuleBase<NewsEntryInfo,int>
    {
        #region Properties

        ViewModelContext viewModelContext;
        protected ViewModelContext ViewModelContext
        {
            get { return viewModelContext ?? (viewModelContext = new ViewModelContext (this)); }
        }

        protected EditNewsEntryTab SelectedTab
        {
            get 
            {
                // get postback initiator
                var eventTarget = Request.Form ["__EVENTTARGET"];
                if (!string.IsNullOrEmpty (eventTarget)) {
                    if (eventTarget.Contains ("$" + urlUrl.ID)) {
                        // urlURL control is on Advanced tab
                        ViewState ["SelectedTab"] = EditNewsEntryTab.Advanced;
                        return EditNewsEntryTab.Advanced;
                    }

                    if (eventTarget.Contains ("$" + comboNewsSourceProvider.ID)) {
                        // comboNewsSourceProvider control is on Citation tab
                        ViewState ["SelectedTab"] = EditNewsEntryTab.Citation;
                        return EditNewsEntryTab.Citation;
                    }

                    if (eventTarget.Contains ("$" + buttonGetModules.ID)) {
                        // buttonGetModules control is on Citation tab
                        ViewState ["SelectedTab"] = EditNewsEntryTab.Citation;
                        return EditNewsEntryTab.Citation;
                    }
                }
                    
                // otherwise, get active tab from viewstate
                var obj = ViewState ["SelectedTab"];
                return (obj != null) ? (EditNewsEntryTab) obj : EditNewsEntryTab.Common;
            }
            set { ViewState ["SelectedTab"] = value; }
        }

        #endregion

        public EditNewsEntry () : base ("entryid", true)
        {
        }

        protected override void OnInit (EventArgs e)
        {
            base.OnInit (e);

            var newsSourceProviders = NewsSourceRepository.Instance.GetSelfSources ();
            newsSourceProviders.Insert (0, new NewsSourceInfo { 
                SourceId = Null.NullInteger, 
                Title = LocalizeString ("NotSelected.Text")
            });

            comboNewsSourceProvider.DataSource = newsSourceProviders;
            comboNewsSourceProvider.DataBind ();

            UpdateNewsSources ();

            pickerImage.FolderPath = NewsConfig.Instance.DefaultImagesPath;
            pickerImage.FileFilter = Globals.glbImageFileTypes;

            // fill weight comboboxes, -1 allow to create hidden news
            for (var i = -1; i <= NewsConfig.Instance.NewsEntry.MaxWeight; i++) {
                comboThematicWeight.Items.Add (i.ToString ());
                comboStructuralWeight.Items.Add (i.ToString ());
            }

            // set default news entry weight
            comboThematicWeight.SelectByValue (NewsConfig.Instance.NewsEntry.DefaultThematicWeight);
            comboStructuralWeight.SelectByValue (NewsConfig.Instance.NewsEntry.DefaultStructuralWeight);

            // localize column headers in the gridview
            gridModules.LocalizeColumnHeaders (".Column", LocalResourceFile);
        }

        protected void comboNewsSourceProvider_SelectedIndexChanged (object sender, EventArgs e)
        {
            // disable handler when loading item
            if (IsPostBack) {
                UpdateNewsSources ();
            }
        }

        protected void buttonGetModules_Click (object sender, EventArgs e)
        {
            var thematicWeight = int.Parse (comboThematicWeight.SelectedValue);
            var structuralWeight = int.Parse (comboStructuralWeight.SelectedValue);
            var terms = termsTerms.Terms;

            gridModules.DataSource = GetStreamModules (thematicWeight, structuralWeight, terms);
            gridModules.DataBind ();
        }

        // REVIEW: Move to business logic layer
        protected IEnumerable<StreamModuleViewModel> GetStreamModules (int thematicWeight, int structuralWeight, IList<Term> terms)
        {
            var moduleController = new ModuleController ();
            return moduleController.GetModulesByDefinition (PortalId, "R7.News.Stream")
                .Cast<ModuleInfo> ()
                .Where (m => !m.IsDeleted)
                .Where (m => StreamModuleViewModel.IsNewsEntryWillBePassedByModule (new StreamSettings (m), 
                    thematicWeight, structuralWeight, terms))
                .Select (m => new StreamModuleViewModel (m, new StreamSettings (m), ViewModelContext, thematicWeight, structuralWeight, terms))
                .OrderBy (m => m.ModuleTitle);
        }

        protected void UpdateNewsSources ()
        {
            var sourceId = TypeUtils.ParseToNullable<int> (comboNewsSourceProvider.SelectedValue);

            var newsSources = NewsSourceRepository.Instance.GetSources (sourceId)
                .OrderBy (ns => ns.Title)
                .ToList ();
            
            newsSources.Insert (0, new NewsSourceInfo { 
                SourceItemId = Null.NullInteger, 
                Title = LocalizeString ("NotSelected.Text")
            });

            comboNewsSource.DataSource = newsSources;
            comboNewsSource.DataBind ();
        }

        #region Implemented abstract and overriden members of EditPortalModuleBase

        protected override void InitControls ()
        {
            InitControls (buttonUpdate, buttonDelete, linkCancel, ctlAudit); 
        }

        protected override void LoadNewItem ()
        {
            termsTerms.PortalId = PortalId;
            termsTerms.Terms = new List<Term> ();
            termsTerms.DataBind ();

            buttonUpdate.Text = LocalizeString ("Add.Text");
        }

        protected override void LoadItem (NewsEntryInfo item)
        {
            // load also content item
            item = (NewsEntryInfo) item.WithContentItem ();

            var image = item.ContentItem.Images.FirstOrDefault ();
            if (image != null) {
                pickerImage.FileID = image.FileId;
            }

            textTitle.Text = item.Title;
            textDescription.Text = item.Description;
            textSortIndex.Text = item.SortIndex.ToString ();

            datetimeThresholdDate.SelectedDate = item.ThresholdDate;
            datetimeDueDate.SelectedDate = item.DueDate;
            datetimeStartDate.SelectedDate = item.StartDate;
            datetimeEndDate.SelectedDate = item.EndDate;

            termsTerms.PortalId = PortalId;
            termsTerms.Terms = item.ContentItem.Terms;
            termsTerms.DataBind ();

            comboNewsSourceProvider.SelectByValue (item.SourceId);
            UpdateNewsSources ();
            comboNewsSource.SelectByValue (item.SourceItemId);

            urlUrl.Url = item.Url;

            comboThematicWeight.SelectByValue (item.ThematicWeight);
            comboStructuralWeight.SelectByValue (item.StructuralWeight);

            ctlAudit.CreatedDate = item.ContentItem.CreatedOnDate.ToLongDateString ();
            ctlAudit.LastModifiedDate = item.ContentItem.LastModifiedOnDate.ToLongDateString ();
            ctlAudit.CreatedByUser = item.ContentItem.CreatedByUser (PortalId).DisplayName;
            ctlAudit.LastModifiedByUser = item.ContentItem.LastModifiedByUser (PortalId).DisplayName;

            buttonUpdate.Text = LocalizeString ("Update.Text");
        }

        private List<IFileInfo> images;

        protected override void BeforeUpdateItem (NewsEntryInfo item)
        {
            if (ItemId == null) {
                images = new List<IFileInfo> ();
            }
            else {
                images = item.ContentItem.Images;
            }

            var imageFile = FileManager.Instance.GetFile (pickerImage.FileID);
            if (imageFile != null) {

                if (images.Count == 0) {
                    images.Add (imageFile);
                }
                else {
                    images.Clear ();
                    images.Add (imageFile);
                }
            }
            else if (images.Count > 0) {
                images.Clear ();
            }

            // fill the object
            item.Title = textTitle.Text.Trim ();
            item.Description = textDescription.Text.Trim ();
            item.SortIndex = int.Parse (textSortIndex.Text);

            item.ThresholdDate = datetimeThresholdDate.SelectedDate;
            item.DueDate = datetimeDueDate.SelectedDate;
            item.StartDate = datetimeStartDate.SelectedDate;
            item.EndDate = datetimeEndDate.SelectedDate;
            item.PortalId = PortalId;

            item.SourceId = TypeUtils.ParseToNullable<int> (comboNewsSourceProvider.SelectedValue);
            item.SourceItemId = TypeUtils.ParseToNullable<int> (comboNewsSource.SelectedValue);

            item.Url = urlUrl.Url;

            item.ThematicWeight = int.Parse (comboThematicWeight.SelectedValue);
            item.StructuralWeight = int.Parse (comboStructuralWeight.SelectedValue);

            if (ModuleConfiguration.ModuleDefinition.DefinitionName == "R7.News.Agent") {
                item.AgentModuleId = ModuleId;
            }   
        }

        protected override NewsEntryInfo GetItem (int itemId)
        {
            return NewsRepository.Instance.GetNewsEntry (itemId, PortalId);
        }

        protected override int AddItem (NewsEntryInfo item)
        {
            NewsRepository.Instance.AddNewsEntry (item, termsTerms.Terms, images, ModuleId, TabId);
            return item.EntryId;
        }

        protected override void UpdateItem (NewsEntryInfo item)
        {
            NewsRepository.Instance.UpdateNewsEntry (item, termsTerms.Terms, ModuleId, TabId);
        }

        protected override void DeleteItem (NewsEntryInfo item)
        {
            NewsRepository.Instance.DeleteNewsEntry (item);
        }

        #endregion
    }
}
