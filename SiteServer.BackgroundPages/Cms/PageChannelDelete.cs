﻿using System;
using System.Collections.Specialized;
using System.Text;
using System.Web.UI.WebControls;
using SiteServer.Utils;
using SiteServer.CMS.Core;
using System.Collections.Generic;
using SiteServer.CMS.Caches;
using SiteServer.CMS.Caches.Content;
using SiteServer.CMS.Core.Create;
using SiteServer.CMS.Database.Core;
using SiteServer.CMS.Fx;

namespace SiteServer.BackgroundPages.Cms
{
	public class PageChannelDelete : BasePageCms
    {
        public Literal LtlPageTitle;
		public RadioButtonList RblRetainFiles;
        public Button BtnDelete;

        private bool _deleteContents;
        private readonly List<string> _nodeNameList = new List<string>();

        public string ReturnUrl { get; private set; }

        public static string GetRedirectUrl(int siteId, string returnUrl)
        {
            return FxUtils.GetCmsUrl(siteId, nameof(PageChannelDelete), new NameValueCollection
            {
                {"ReturnUrl", StringUtils.ValueToUrl(returnUrl)}
            });
        }

		public void Page_Load(object sender, EventArgs e)
        {
            if (IsForbidden) return;

            FxUtils.CheckRequestParameter("siteId", "ReturnUrl");
            ReturnUrl = StringUtils.ValueFromUrl(AuthRequest.GetQueryString("ReturnUrl"));
            _deleteContents = AuthRequest.GetQueryBool("DeleteContents");

            if (IsPostBack) return;

            var channelIdList = TranslateUtils.StringCollectionToIntList(AuthRequest.GetQueryString("ChannelIDCollection"));
            channelIdList.Sort();
            channelIdList.Reverse();
            foreach (var channelId in channelIdList)
            {
                if (channelId == SiteId) continue;
                if (!HasChannelPermissions(channelId, ConfigManager.ChannelPermissions.ChannelDelete)) continue;

                var channelInfo = ChannelManager.GetChannelInfo(SiteId, channelId);
                var onlyAdminId = AuthRequest.AdminPermissionsImpl.GetOnlyAdminId(SiteId, channelId);
                var displayName = channelInfo.ChannelName;
                var count = ContentManager.GetCount(SiteInfo, channelInfo, onlyAdminId);
                if (count > 0)
                {
                    displayName += $"({count})";
                }
                _nodeNameList.Add(displayName);
            }

            if (_nodeNameList.Count == 0)
            {
                BtnDelete.Enabled = false;
            }
            else
            {
                if (_deleteContents)
                {
                    LtlPageTitle.Text = "删除内容";
                    InfoMessage(
                        $"此操作将会删除栏目“{TranslateUtils.ObjectCollectionToString(_nodeNameList)}”下的所有内容，确认吗？");
                }
                else
                {
                    LtlPageTitle.Text = "删除栏目";
                    InfoMessage(
                        $"此操作将会删除栏目“{TranslateUtils.ObjectCollectionToString(_nodeNameList)}”及包含的下级栏目，确认吗？");
                }
            }
        }

        public void Delete_OnClick(object sender, EventArgs e)
        {
            if (!Page.IsPostBack || !Page.IsValid) return;

            try
            {
                var channelIdList = TranslateUtils.StringCollectionToIntList(AuthRequest.GetQueryString("ChannelIDCollection"));
                channelIdList.Sort();
                channelIdList.Reverse();

                var channelIdListToDelete = new List<int>();
                foreach (var channelId in channelIdList)
                {
                    if (channelId == SiteId) continue;
                    if (HasChannelPermissions(channelId, ConfigManager.ChannelPermissions.ChannelDelete))
                    {
                        channelIdListToDelete.Add(channelId);
                    }
                }

                var builder = new StringBuilder();
                foreach (var channelId in channelIdListToDelete)
                {
                    builder.Append(ChannelManager.GetChannelName(SiteId, channelId)).Append(",");
                }

                if (builder.Length > 0)
                {
                    builder.Length -= 1;
                }

                if (_deleteContents)
                {
                    SuccessMessage(bool.Parse(RblRetainFiles.SelectedValue) == false
                        ? "成功删除内容以及生成页面！"
                        : "成功删除内容，生成页面未被删除！");

                    foreach (var channelId in channelIdListToDelete)
                    {
                        var channelInfo = ChannelManager.GetChannelInfo(SiteId, channelId);
                        var contentIdList = channelInfo.ContentRepository.GetContentIdList(channelId);
                        DeleteManager.DeleteContents(SiteInfo, channelId, contentIdList);
                        channelInfo.ContentRepository.UpdateTrashContents(SiteId, channelId, contentIdList);

                        //var tableName = ChannelManager.GetTableName(SiteInfo, channelId);
                        //var contentIdList = DataProvider.ContentRepository.GetContentIdList(tableName, channelId);
                        //DeleteManager.DeleteContents(SiteInfo, channelId, contentIdList);
                        //DataProvider.ContentRepository.UpdateTrashContents(SiteId, channelId, tableName, contentIdList);
                    }

                    AuthRequest.AddSiteLog(SiteId, "清空栏目下的内容", $"栏目:{builder}");
                }
                else
                {
                    if (bool.Parse(RblRetainFiles.SelectedValue) == false)
                    {
                        DeleteManager.DeleteChannels(SiteInfo, channelIdListToDelete);
                        SuccessMessage("成功删除栏目以及相关生成页面！");
                    }
                    else
                    {
                        SuccessMessage("成功删除栏目，相关生成页面未被删除！");
                    }

                    foreach (var channelId in channelIdListToDelete)
                    {
                        var channelInfo = ChannelManager.GetChannelInfo(SiteId, channelId);
                        channelInfo.ContentRepository.UpdateTrashContentsByChannelId(SiteId, channelId);
                        DataProvider.Channel.Delete(SiteId, channelId);
                    }

                    AuthRequest.AddSiteLog(SiteId, "删除栏目", $"栏目:{builder}");
                }

                AddWaitAndRedirectScript(ReturnUrl);
            }
            catch (Exception ex)
            {
                FailMessage(ex, _deleteContents ? "删除内容失败！" : "删除栏目失败！");

                LogUtils.AddErrorLog(ex);
            }
        }
    }
}
