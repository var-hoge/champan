using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ���������C#�X�N���v�g�̃e���v���[�g�J�X�^�}�C�Y
/// </summary>

namespace TadaLib.Editor
{
    /// <summary>
    /// ���ʏ���
    /// </summary>
    class TemplateScriptCustomizerCommon
    {
        public static void CreateCusomizedScriptImpl<T>(string templateFile) where T : UnityEditor.ProjectWindowCallback.EndNameEditAction
        {
            var resourceFile = Path.Combine(
                Application.dataPath,
                $"TadaLib/Scripts/Editor/Data/{templateFile}");

            // unity�ŗp�ӂ���Ă���C#�̃A�C�R���𗘗p����
            var csIcon =
                EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;

            // ScriptableObject�̃C���X�^���X�Ƃ��č쐬����
            var endNameEditAction =
                ScriptableObject.CreateInstance<T>();

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,                                                      // �����K���ł����v
                endNameEditAction,                                      // ���s����Action�̎w��
                "NewBehaviourScript.cs",                                // �f�t�H���g��Action�͌Ă΂�Ȃ����ߖ��O�������Ŏw��
                csIcon,                                                 // �A�C�R���̐ݒ�
                resourceFile);                                          // �g�p����X�N���v�g�e���v���[�g
        }

        public static void ActionImpl(string text, string pathName)
        {
            var encoding = new System.Text.UTF8Encoding(true, false);

            File.WriteAllText(pathName, text, encoding);

            AssetDatabase.ImportAsset(pathName);
            var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(pathName);
            ProjectWindowUtil.ShowCreatedAsset(asset);
        }
    }

    /// <summary>
    /// �R���|�[�l���g�p
    /// </summary>
    public class TemplateScriptCustomizerComponent : UnityEditor.ProjectWindowCallback.EndNameEditAction
    {
        [MenuItem("Assets/Create/Component C# Script", isValidateFunction: false, priority: 76)]
        private static void CreateCustomizedScript()
        {
            TemplateScriptCustomizerCommon.CreateCusomizedScriptImpl<TemplateScriptCustomizerComponent>(
                "C# Script-NewComponentScript.cs.txt");
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var text = File.ReadAllText(resourceFile);

            var directoryName = Path.GetDirectoryName(pathName).Replace(@"\", ".").Replace("App.", "").Replace("Assets.", "").Replace("Scripts.","");
            var name = Path.GetFileNameWithoutExtension(pathName);
            var scriptName = name.Replace(" ", "");

            text = text.Replace("#DIRECTORYNAME#", directoryName);
            text = text.Replace("#SCRIPTNAME#", scriptName);
            text = text.Replace("#NOTRIM#", "");

            TemplateScriptCustomizerCommon.ActionImpl(text, pathName);
        }
    }

    /// <summary>
    /// �X�e�[�g�p
    /// </summary>
    public class TemplateScriptCustomizerState : UnityEditor.ProjectWindowCallback.EndNameEditAction
    {
        [MenuItem("Assets/Create/State C# Script", isValidateFunction: false, priority: 76)]
        private static void CreateCustomizedScript()
        {
            TemplateScriptCustomizerCommon.CreateCusomizedScriptImpl<TemplateScriptCustomizerState>(
                "C# Script-NewStateScript.cs.txt");
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var text = File.ReadAllText(resourceFile);

            var directoryName = Path.GetDirectoryName(pathName).Replace(@"\", ".").Replace("App.", "").Replace("Assets.", "").Replace("Scripts.", "");
            var name = Path.GetFileNameWithoutExtension(pathName);
            var scriptName = name.Replace(" ", "");

            text = text.Replace("#DIRECTORYNAME#", directoryName);
            text = text.Replace("#SCRIPTNAME#", scriptName);
            text = text.Replace("#NOTRIM#", "");

            TemplateScriptCustomizerCommon.ActionImpl(text, pathName);
        }
    }

    /// <summary>
    /// Manager�R���|�[�l���g�p
    /// </summary>
    public class TemplateScriptCustomizerManagerComponent : UnityEditor.ProjectWindowCallback.EndNameEditAction
    {
        [MenuItem("Assets/Create/ManagerComponent C# Script", isValidateFunction: false, priority: 76)]
        private static void CreateCustomizedScript()
        {
            TemplateScriptCustomizerCommon.CreateCusomizedScriptImpl<TemplateScriptCustomizerComponent>(
                "C# Script-NewManagerComponentScript.cs.txt");
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var text = File.ReadAllText(resourceFile);

            var directoryName = Path.GetDirectoryName(pathName).Replace(@"\", ".").Replace("App.", "").Replace("Assets.", "").Replace("Scripts.", "");
            var name = Path.GetFileNameWithoutExtension(pathName);
            var scriptName = name.Replace(" ", "");

            text = text.Replace("#DIRECTORYNAME#", directoryName);
            text = text.Replace("#SCRIPTNAME#", scriptName);
            text = text.Replace("#NOTRIM#", "");

            TemplateScriptCustomizerCommon.ActionImpl(text, pathName);
        }
    }

    /// <summary>
    /// ���̂ق��p
    /// </summary>
    public class TemplateScriptCustomizerDefault : UnityEditor.ProjectWindowCallback.EndNameEditAction
    {
        [MenuItem("Assets/Create/Default C# Script", isValidateFunction: false, priority: 76)]
        private static void CreateCustomizedScript()
        {
            TemplateScriptCustomizerCommon.CreateCusomizedScriptImpl<TemplateScriptCustomizerDefault>(
                "C# Script-NewDefaultScript.cs.txt");
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var text = File.ReadAllText(resourceFile);

            var directoryName = Path.GetDirectoryName(pathName).Replace(@"\", ".").Replace("App.", "").Replace("Assets.", "").Replace("Scripts.", "");
            var name = Path.GetFileNameWithoutExtension(pathName);
            var scriptName = name.Replace(" ", "");

            text = text.Replace("#DIRECTORYNAME#", directoryName);
            text = text.Replace("#SCRIPTNAME#", scriptName);
            text = text.Replace("#NOTRIM#", "");

            TemplateScriptCustomizerCommon.ActionImpl(text, pathName);
        }
    }
}
