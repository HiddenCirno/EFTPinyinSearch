using EFT.UI;
using HarmonyLib;
using System;
using System.Reflection;

namespace EFTPinyinSearch
{
    internal class PinyinSearchPatch
    {
        //参考了小火山的搜索优化, 去除搜索长度限制
        [HarmonyPatch(typeof(BrowseCategoriesPanel), "method_2")]
        internal static class BrowseCategoriesPanelInputPadPatch
        {
            private static void Prefix(ref string arg)
            {
                if (!string.IsNullOrEmpty(arg))
                {
                    if (!arg.StartsWith("#", StringComparison.Ordinal) && arg.Length < 3)
                    {
                        arg = arg.PadRight(3, ' ');
                    }
                }
            }
        }
        //跳蚤市场和手册的搜索逻辑
        [HarmonyPatch]
        internal static class PinyinSearchPredicatePatch
        {
            //缓存
            private static Type _closureType;
            private static FieldInfo _panelField;
            private static FieldInfo _valueField;
            //Class3102是编译时生成的自动化类型, 无法直接访问, 动态捕获方法
            private static MethodBase TargetMethod()
            {
                _closureType = AccessTools.Inner(typeof(BrowseCategoriesPanel), "Class3102");
                _panelField = AccessTools.Field(_closureType, "browseCategoriesPanel_0");
                _valueField = AccessTools.Field(_closureType, "value");
                return AccessTools.Method(_closureType, "method_1");
            }
            //Prefix注入搜索逻辑
            private static bool Prefix(object __instance, EntityNodeClass x, ref bool __result)
            {
                //传参为空, 搜索无结果
                if (__instance == null || x == null || x.Data == null) { __result = false; return false; }
                //从反射获取搜索框实例, 检查结果过滤器
                BrowseCategoriesPanel panel = _panelField?.GetValue(__instance) as BrowseCategoriesPanel;
                if (panel == null || !panel.Allowed(x)) { __result = false; return false; }
                //从反射获取搜索框的内容
                string text = _valueField?.GetValue(__instance) as string;
                string searchValue = string.IsNullOrEmpty(text) ? string.Empty : text.Trim().ToLower();
                //字符长度为0, 无结果
                if (searchValue.Length == 0) { __result = false; return false; }
                //初始化字典
                PinyinCacheManager.InitIfNeeded();
                //获取物品的本地化key和名字
                string itemLocKey = x.Data.Name;
                string localizedName = itemLocKey.Localized(null);
                //原版匹配逻辑(即关键词匹配)
                if (!string.IsNullOrEmpty(localizedName) && localizedName.IndexOf(searchValue, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    __result = true; return false;
                }
                //拼音搜索逻辑
                if (itemLocKey.Length >= 24)
                {
                    //从本地化key截取物品ID
                    string mongoId = itemLocKey.Substring(0, 24);
                    //从字典读取拼音
                    if (PinyinCacheManager.Dict.TryGetValue(mongoId, out string pinyinData))
                    {
                        //比对拼音关键字
                        if (pinyinData.IndexOf(searchValue, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            __result = true; return false;
                        }
                    }
                }
                //无结果
                __result = false; return false;
            }
        }
        //仓库搜索逻辑
        [HarmonyPatch(typeof(EFT.UI.StashSearchWindow), "method_25")]
        internal static class StashSearchWindow_method25_Patch
        {
            //缓存
            private static FieldInfo _list2Field;

            static StashSearchWindow_method25_Patch()
            {
                //反射获取白名单
                _list2Field = AccessTools.Field(typeof(EFT.UI.StashSearchWindow), "list_2");
            }
            //Postfix
            //仓库搜索和跳蚤搜索逻辑不同
            //仓库搜索最终给出一个符合条件的白名单
            //我们不完全覆盖逻辑, 而是将拼音匹配结果注入白名单
            private static void Postfix(EFT.UI.StashSearchWindow __instance, string text)
            {
                //空值检查
                if (string.IsNullOrEmpty(text)) return;
                string searchValue = text.Trim().ToLower();
                //初始化字典
                //两边各调用一次初始化, 防空
                PinyinCacheManager.InitIfNeeded();
                //反射获取白名单
                System.Collections.IList whitelist = _list2Field.GetValue(__instance) as System.Collections.IList;
                if (whitelist == null) return;
                //获取MongoId类型
                Type mongoIdType = whitelist.GetType().GetGenericArguments()[0];
                //字典匹配
                foreach (var kvp in PinyinCacheManager.Dict)
                {
                    if (kvp.Value.IndexOf(searchValue, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        object mongoIdObj = Activator.CreateInstance(mongoIdType, kvp.Key);
                        if (!whitelist.Contains(mongoIdObj))
                        {
                            //将结果添加到白名单
                            whitelist.Add(mongoIdObj);
                        }
                    }
                }
                //更新显示结果
                __instance.ItemsUpdateRequired = true;
            }
        }
    }
}
