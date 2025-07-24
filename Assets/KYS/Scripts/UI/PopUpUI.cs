using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KYS
{
    public class PopUpUI : MonoBehaviour
    {
        // TODO: 팝업 되면 팝업 판넬 범위 바깥을 1. 클릭하지 못하게 막거나, 2. 클릭 시 팝업 이전으로 돌아가게 한다.
        private Stack<BaseUI> stack = new Stack<BaseUI>();
        public static bool IsPopUpActive { get; private set; } = false;

        [SerializeField] GameObject blocker;
        public void OnApplicationQuit()
        {
            DestroyImmediate(gameObject);
        }
        public void PushUIStack(BaseUI ui)
        {
            IsPopUpActive = true;
            if (stack.Count > 0)
            {
                BaseUI top = stack.Peek();
                top.gameObject.SetActive(false);
            }


            stack.Push(ui);
            blocker.SetActive(true);
        }
        public void PopUIStack()
        {
            if (stack.Count == 1)
            {
                IsPopUpActive = false;
            }
            if (stack.Count <= 0)
            {
                IsPopUpActive = false;
                return;
            }

            Destroy(stack.Pop().gameObject);
            if (stack.Count > 0)
            {
                BaseUI top = stack.Peek();
                top.gameObject.SetActive(true);
            }
            else
            {


                blocker.SetActive(false);
            }

        }
        public int StackCount()
        {
            return stack.Count;
        }
    }


}