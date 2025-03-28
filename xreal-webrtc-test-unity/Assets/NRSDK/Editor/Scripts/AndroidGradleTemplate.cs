﻿/****************************************************************************
* Copyright 2019 Xreal Techonology Limited.All rights reserved.
*
* This file is part of NRSDK.
*
* https://www.xreal.com/        
*
*****************************************************************************/

namespace NRKernal
{
    using System;
    using System.Text;
    using System.Xml;
    using System.IO;
    using System.Linq.Expressions;
    using System.Collections.Generic;
    using UnityEngine;
    using System.Text.RegularExpressions;
    using System.Linq;

    /// <summary> A list of the android. </summary>
    internal class AndroidGradleTemplate
    {
        private enum EPatcherType
        {
            EPT_NONE = 0,
            EPT_PLUGIN_VERSION = 1,
            EPT_ADD_DEPENDENCIES = 2,
            EPT_REMOVE_DEPENDENCIES = 3,
            EPT_ADD_SUPPORT = 4,
            EPT_SET_PACKINGOPTIONS = 5,
        }
        private interface IGradlePatcher
        {
            void PreprocessLine(string line);
            bool ProcessLine(string line, ref string result);
            string ProcessEntireContent(string content);
        }

        private class GradlePluginVersionPatcher : IGradlePatcher
        {
            const string PLUGIN_VERSION_TOKEN = "com.android.tools.build:gradle:";
            private int mMajorVersionNum;
            private int mMiddleVersionNum;
            private int mMinorVersionNum;
            public GradlePluginVersionPatcher()
            {
                mMajorVersionNum = 0;
                mMiddleVersionNum = 0;
                mMinorVersionNum = 0;
            }
            public void SetMinPluginVersion(int major, int middle, int minor)
            {
                mMajorVersionNum = major;
                mMiddleVersionNum = middle;
                mMinorVersionNum = minor;
            }

            public void PreprocessLine(string line)
            {

            }

            public bool ProcessLine(string line, ref string result)
            {
                bool updateVersion = false;
                var idx = line.IndexOf(PLUGIN_VERSION_TOKEN);
                if (idx > 0)
                {
                    string subLine = line.Substring(idx + PLUGIN_VERSION_TOKEN.Length);
                    string subVersion = subLine.Substring(0, subLine.IndexOf('\''));
                    Debug.LogFormat("subVersion : {0}", subVersion);

                    string[] versions = subVersion.Split('.');
                    if (versions.Length == 3)
                    {
                        int.TryParse(versions[0], out int vMain);
                        int.TryParse(versions[1], out int vMiddle);
                        int.TryParse(versions[2], out int vMin);
                        
                        if (vMain < mMajorVersionNum)
                        {
                            updateVersion = true;
                        }
                        else if(vMain == mMajorVersionNum)
                        {
                            if(vMiddle < mMiddleVersionNum)
                            {
                                updateVersion = true;
                            }
                            else if(vMiddle == mMiddleVersionNum)
                            {
                                if(vMin < mMinorVersionNum)
                                {
                                    updateVersion = true;
                                }
                            }
                        }

                        if (updateVersion)
                        {
                            string version = string.Format("{0}.{1}.{2}", mMajorVersionNum, mMiddleVersionNum,
                                mMinorVersionNum);
                            result = line.Replace(subVersion, version);
                            Debug.LogFormat("update gradle setting : {0} --> {1}", subVersion, version);
                        }
                    }
                }
                return updateVersion;
            }

            public string ProcessEntireContent(string content)
            {
                return content;
            }
        }

        private class GradleAddDependenciesPatcher : IGradlePatcher
        {
            const string DEPS_MARK = "**DEPS**";
            private List<string> mDependencies;
            public GradleAddDependenciesPatcher()
            {
                mDependencies = new List<string>();
            }

            public void AddDependency(string dependency)
            {
                mDependencies.Add(dependency);
            }

            public void PreprocessLine(string line)
            {
                for(int i = mDependencies.Count - 1; i >= 0; i--)
                {
                    if(line.Contains(mDependencies[i]))
                    {
                        //this dependency is already in the gradle file
                        mDependencies.RemoveAt(i);
                    }
                }
            }
            public bool ProcessLine(string line, ref string result)
            {
                if(mDependencies.Count > 0 && line.Contains(DEPS_MARK))
                {
                    result = "    " + string.Join("\n    ", mDependencies);
                    result = result + "\n" + line;
                    return true;
                }
                return false;
            }

            public string ProcessEntireContent(string content)
            {
                return content;
            }
        }

        private class GradlePackagingOptionsPatcher : IGradlePatcher
        {
            string[] DEPS_MARKS = new string[2] { "**PACKAGING**", "**PACKAGING_OPTIONS**" };
            string mPackingOptionsRegex = @"(.*packagingOptions\s+\{)(.*?)(\}.*)";
            List<string> mOptionLines = new List<string>();

            bool mUsingDefaultPackingOptions = false;
            string DefaultPackingOptionsKey = string.Empty;

            public void SetOptions(string options)
            {
                mOptionLines.Add(options);
            }

            public void PreprocessLine(string line)
            {
                for (int i = mOptionLines.Count - 1; i >= 0; i--)
                {
                    if (line.Contains(mOptionLines[i]))
                    {
                        mOptionLines.RemoveAt(i);
                    }
                }

                if (mUsingDefaultPackingOptions)
                    return;

                foreach (var key in DEPS_MARKS)
                {
                    if (line.Contains(key))
                    {
                        DefaultPackingOptionsKey = key;
                        mUsingDefaultPackingOptions = true;
                        break;
                    }
                }
            }
            public bool ProcessLine(string line, ref string result)
            {
                if (mOptionLines.Count <= 0)
                    return false;

                if (mUsingDefaultPackingOptions && !string.IsNullOrEmpty(DefaultPackingOptionsKey))
                {
                    if (line.Contains(DefaultPackingOptionsKey))
                    {
                        string completeOptions = string.Format("\npackagingOptions {{\n{0}\n}}", string.Join(Environment.NewLine, mOptionLines));
                        result += completeOptions;
                        return true;
                    }
                }
                return false;
            }

            public string ProcessEntireContent(string content)
            {
                if (!mUsingDefaultPackingOptions && mOptionLines.Count > 0)
                {
                    Match match = Regex.Match(content, mPackingOptionsRegex, RegexOptions.Singleline);
                    if (match.Success)
                    {
                        return match.Groups[1].Value + match.Groups[2].Value + string.Join(Environment.NewLine, mOptionLines) + match.Groups[3].Value;
                    }
                }
                return content;
            }
        }

        private class GradleRemoveDependenciesPatcher : IGradlePatcher
        {
            private List<string> mDependencies;
            public GradleRemoveDependenciesPatcher()
            {
                mDependencies = new List<string>();
            }

            public void RemoveDependency(string dependency)
            {
                mDependencies.Add(dependency);
            }

            public void PreprocessLine(string line)
            {

            }

            public bool ProcessLine(string line, ref string result)
            {
                bool includeDeps = false;
                for (int i = 0; i < mDependencies.Count; i++)
                {
                    if (line.Contains(mDependencies[i]))
                    {
                        includeDeps = true;
                        //remove this line
                        result = null;
                        break;
                    }
                }
                return includeDeps;
            }

            public string ProcessEntireContent(string content)
            {
                return content;
            }
        }

        private class GradleAddSupportPatcher : IGradlePatcher
        {
            private Dictionary<string, bool> mKeyTokenAlreadyInFile = new Dictionary<string, bool>();
            private bool mIsFirst = true;
            private List<string> mTokenList = new List<string>();
            public void PreprocessLine(string line)
            {
                for(int i = 0; i < mTokenList.Count; i++)
                {
                    var token = mTokenList[i];
                    if (line.Contains(token))
                    {
                        mKeyTokenAlreadyInFile[token] = true;
                    }
                }
            }

            public bool ProcessLine(string line, ref string result)
            {
                string tempLine = "";
                if (mIsFirst)
                {
                    tempLine = GetSupportStringNotInFile();
                    mIsFirst = false;
                }

                foreach (var pair in mKeyTokenAlreadyInFile)
                {
                    if (pair.Value)
                    {
                        if (line.Contains(pair.Key))
                        {
                            result = tempLine + pair.Key + "=true";
                            return true;
                        }
                    }
                }
                result = tempLine + line;
                return false;
            }

            public string ProcessEntireContent(string content)
            {
                return content;
            }

            private string GetSupportStringNotInFile()
            {
                string line = "";
                foreach (var pair in mKeyTokenAlreadyInFile)
                {
                    if (!pair.Value)
                    {
                        line = string.Format("{0}=true\n{1}", pair.Key, line);
                    }
                }
                return line;
            }

            public void AddSupport(string keyToken)
            {
                mKeyTokenAlreadyInFile.Add(keyToken, false);
                mTokenList.Add(keyToken);
            }
        }

        Dictionary<EPatcherType, IGradlePatcher> mPatchers = null;
        string m_Path;
        public AndroidGradleTemplate(string path)
        {
            m_Path = path;
            mPatchers = new Dictionary<EPatcherType, IGradlePatcher>();
        }

        private T GetOrAddPatcher<T>(EPatcherType type) where T : IGradlePatcher, new()
        {
            if (!mPatchers.TryGetValue(type, out IGradlePatcher patcher))
            {
                patcher = new T();
                mPatchers.Add(type, patcher);
            }
            return (T)patcher;
        }
        public void SetMinPluginVersion(int major, int middle, int minor)
        {
            GradlePluginVersionPatcher pluginVersionPatcher = GetOrAddPatcher<GradlePluginVersionPatcher>(
                EPatcherType.EPT_PLUGIN_VERSION);
            pluginVersionPatcher.SetMinPluginVersion(major, middle, minor);
        }

        public void AddDenpendency(string dependency)
        {
            GradleAddDependenciesPatcher addDepPatcher = GetOrAddPatcher<GradleAddDependenciesPatcher>(
                EPatcherType.EPT_ADD_DEPENDENCIES);
            addDepPatcher.AddDependency(dependency);
        }

        public void RemoveDependency(string dependency)
        {
            GradleRemoveDependenciesPatcher removeDepPatcher = GetOrAddPatcher<GradleRemoveDependenciesPatcher>(
                EPatcherType.EPT_REMOVE_DEPENDENCIES);
            removeDepPatcher.RemoveDependency(dependency);
        }

        public void AddSupport(string keyToken)
        {
            GradleAddSupportPatcher addSupportPatcher = GetOrAddPatcher<GradleAddSupportPatcher>(
                EPatcherType.EPT_ADD_SUPPORT);
            addSupportPatcher.AddSupport(keyToken);
        }

        public void SetPackingOptions(string options)
        {
            GradlePackagingOptionsPatcher packingOptionsPather = GetOrAddPatcher<GradlePackagingOptionsPatcher>(
                EPatcherType.EPT_SET_PACKINGOPTIONS);
            packingOptionsPather.SetOptions(options);
        }

        public void PreprocessGradleFile()
        {
            if (mPatchers.Count <= 0)
                return;
            try
            {
                List<string> content = new List<string>();
                var lines = File.ReadAllLines(m_Path);
                string newLine = null;
                foreach (string line in lines)
                {
                    foreach (var pair in mPatchers)
                    {
                        pair.Value.PreprocessLine(line);
                    }
                }

                foreach (string line in lines)
                {
                    newLine = line;
                    foreach (var pair in mPatchers)
                    {
                        if(pair.Value.ProcessLine(line, ref newLine))
                        {
                            break;
                        }
                    }
                    //Original line may be empty, not null
                    if (newLine != null)
                    {
                        content.Add(newLine);
                    }
                }

                //process entire file content
                string text = string.Join(Environment.NewLine, content);
                foreach (var pair in mPatchers)
                {
                    text = pair.Value.ProcessEntireContent(text);
                }
                File.WriteAllText(m_Path, text);
            }
            catch (Exception ex)
            {
                Debug.LogErrorFormat("PreprocessGradleFile exception : {0}", ex.Message);
            }
        }
    }

}
