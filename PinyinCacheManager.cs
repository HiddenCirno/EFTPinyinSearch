using System;
using System.Collections.Generic;
using ToolGood.Words.Pinyin;

namespace EFTPinyinSearch
{
    //缓存管理器
    internal static class PinyinCacheManager
    {
        //拼音字典和初始化开关
        public static Dictionary<string, string> Dict = new Dictionary<string, string>();
        public static bool IsInitialized = false;
        //初始化字典
        public static void InitIfNeeded()
        {
            //已经完成初始化
            if (IsInitialized) return;
            //防止报错
            try
            {
                Console.WriteLine($"[{PluginsInfo.NAME}] 首次搜索触发，正在初始化全局拼音字典...");
                //从LocaleManager的全局单例获取字典
                var localeManager = LocaleManagerClass.LocaleManagerClass;
                if (localeManager == null) return;
                //LocaleManager存储的当前语言key
                string currentLang = localeManager.String_0;
                if (localeManager.Dictionary_4.TryGetValue(currentLang, out var currentLocaleDict))
                {
                    foreach (var kvp in currentLocaleDict)
                    {
                        string key = kvp.Key;
                        //比正则匹配更高效的过滤方法, 塔科夫的物品本地化key为固定的格式, 24位MongoId+空格+Name
                        //检查长度为29且结尾为空格Name的key并提取前24为作为物品ID即可
                        if (key.Length == 29 && key.EndsWith(" Name", StringComparison.Ordinal))
                        {
                            string mongoId = key.Substring(0, 24);
                            string chineseName = kvp.Value;
                            //跳过空值
                            if (string.IsNullOrEmpty(chineseName)) continue;
                            //生成全拼和简拼, 存入字典
                            string fullPinyin = WordsHelper.GetPinyin(chineseName, "").ToLower();
                            string firstPinyin = WordsHelper.GetFirstPinyin(chineseName).ToLower();
                            Dict[mongoId] = $"{fullPinyin}|{firstPinyin}";
                        }
                    }
                    Console.WriteLine($"[{PluginsInfo.NAME}] 拼音字典初始化成功！共载入 {Dict.Count} 条物品数据。");
                }
                //完成初始化
                IsInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{PluginsInfo.NAME}] 初始化字典时发生错误: {ex}");
                //防死循环
                IsInitialized = true;
            }
        }
    }
}
