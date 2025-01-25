using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ���͏����Ǘ�����N���X
/// ��s���͂ȂǂɑΉ����Ă���
/// ��s���͂́C�W�����v�ƃ_�b�V���̂ݑΉ�
/// 
/// ��s���͂Ɋւ���
/// �E�^�C���X�P�[�����ʏ�Ƃ͈قȂ������͂ǂ��Ȃ邩
/// -> �^�C���X�P�[�����l�������C��Ƀ^�C���X�P�[����1�̂Ƃ��Ɠ�������������
/// 
/// </summary>

namespace TadaLib.Input
{

    public interface IInput
    {
        public bool ActionEnabled { get; set; }

        // ���͏�Ԃ����Z�b�g����
        public void ResetInput();

        /// <summary>
        /// �w�肵���{�^�������͂��ꂽ�����擾����
        /// </summary>
        /// <param name="code">�{�^��</param>
        /// <param name="precedeSec">��s���͎���</param>
        /// <returns></returns>
        public bool GetButtonDown(ButtonCode code, float precedeSec);
        /// <summary>
        /// �w�肵���{�^�������͂���Ă��邩���擾����
        /// </summary>
        /// <param name="code">�{�^��</param>
        /// <param name="precedeSec">��s���͎���</param>
        /// <returns></returns>
        public bool GetButton(ButtonCode code, float precedeSec);

        /// <summary>
        /// �w�肵���{�^���̓��͂������ꂽ�����擾����
        /// </summary>
        /// <param name="code">�{�^��</param>
        /// <param name="precedeSec">��s���͎���</param>
        /// <returns></returns>
        public bool GetButtonUp(ButtonCode code, float precedeSec);

        /// <summary>
        /// �ߋ��̓��̓t���O��S�ė��Ă�
        /// </summary>
        /// <param name="code"></param>
        public void ForceFlagOnHistory(ButtonCode code);

        /// <summary>
        /// �ߋ��̓��̓t���O��S�č~�낷
        /// </summary>
        /// <param name="code"></param>
        public void ForceFlagOffHistory(ButtonCode code);

        /// <summary>
        /// �w�肵���{�^�������͂���Ă��邩���擾����
        /// </summary>
        /// <param name="code">�{�^��</param>
        /// <returns></returns>
        public float GetAxis(AxisCode code);
    }
}