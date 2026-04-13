using UnityEngine;
using DG.Tweening;
using System;

namespace IdleGame.UI
{
    /// <summary>
    /// 封装常用 DOTween 动画，供其他脚本复用。
    /// 所有动画方法返回 Tween 以便链式调用。
    /// </summary>
    public static class DOTweenAnimations
    {
        // 默认动画时长
        private const float DefaultDuration = 0.3f;

        #region Slide Animations

        /// <summary>
        /// 从下往上滑入
        /// </summary>
        /// <param name="rectTransform">目标 RectTransform</param>
        /// <param name="duration">动画时长（秒）</param>
        /// <param name="tweenId">可选的 Tween Id，用于批量管理</param>
        /// <returns>Tween 以便链式调用</returns>
        public static Tween SlideInUp(RectTransform rectTransform, float duration = DefaultDuration, string tweenId = null)
        {
            if (rectTransform == null) return null;

            // 记录原始底部锚点高度，滑入终点为 0
            float startY = -rectTransform.rect.height;
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, startY);
            rectTransform.gameObject.SetActive(true);

            var tween = rectTransform
                .DOAnchorPosY(0f, duration)
                .SetEase(Ease.OutBack);
            
            if (!string.IsNullOrEmpty(tweenId))
                tween.SetId(tweenId);
            
            return tween;
        }

        /// <summary>
        /// 向上滑出（滑出后隐藏）
        /// </summary>
        /// <param name="rectTransform">目标 RectTransform</param>
        /// <param name="duration">动画时长（秒）</param>
        /// <returns>Tween 以便链式调用</returns>
        public static Tween SlideOutUp(RectTransform rectTransform, float duration = DefaultDuration)
        {
            if (rectTransform == null) return null;

            float endY = rectTransform.rect.height;

            var tween = rectTransform
                .DOAnchorPosY(endY, duration)
                .SetEase(Ease.InBack);

            tween.OnComplete(() => rectTransform.gameObject.SetActive(false));
            return tween;
        }

        /// <summary>
        /// 从上往下滑入
        /// </summary>
        public static Tween SlideInDown(RectTransform rectTransform, float duration = DefaultDuration)
        {
            if (rectTransform == null) return null;

            float startY = rectTransform.rect.height;
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, startY);
            rectTransform.gameObject.SetActive(true);

            return rectTransform
                .DOAnchorPosY(0f, duration)
                .SetEase(Ease.OutBack);
        }

        /// <summary>
        /// 向下滑出（滑出后隐藏）
        /// </summary>
        public static Tween SlideOutDown(RectTransform rectTransform, float duration = DefaultDuration)
        {
            if (rectTransform == null) return null;

            float endY = -rectTransform.rect.height;

            var tween = rectTransform
                .DOAnchorPosY(endY, duration)
                .SetEase(Ease.InBack);

            tween.OnComplete(() => rectTransform.gameObject.SetActive(false));
            return tween;
        }

        #endregion

        #region Scale Animations

        /// <summary>
        /// 缩放弹入（0 → 1.1 → 1）
        /// </summary>
        /// <param name="rectTransform">目标 RectTransform</param>
        /// <param name="duration">动画时长（秒）</param>
        /// <returns>Tween 以便链式调用</returns>
        public static Tween ScaleIn(RectTransform rectTransform, float duration = DefaultDuration)
        {
            if (rectTransform == null) return null;

            rectTransform.localScale = Vector3.zero;
            rectTransform.gameObject.SetActive(true);

            return rectTransform
                .DOScale(1f, duration)
                .SetEase(Ease.OutBack);
        }

        /// <summary>
        /// 缩放弹出（1 → 0）
        /// </summary>
        /// <param name="rectTransform">目标 RectTransform</param>
        /// <param name="duration">动画时长（秒）</param>
        /// <returns>Tween 以便链式调用</returns>
        public static Tween ScaleOut(RectTransform rectTransform, float duration = DefaultDuration)
        {
            if (rectTransform == null) return null;

            var tween = rectTransform
                .DOScale(0f, duration)
                .SetEase(Ease.InBack);

            tween.OnComplete(() => rectTransform.gameObject.SetActive(false));
            return tween;
        }

        #endregion

        #region Effect Animations

        /// <summary>
        /// 脉冲动画（缩放 1 → 1.15 → 1 循环）
        /// </summary>
        /// <param name="rectTransform">目标 RectTransform</param>
        /// <returns>Tween（需手动 Kill）</returns>
        public static Tween Pulse(RectTransform rectTransform)
        {
            if (rectTransform == null) return null;

            return rectTransform
                .DOScale(1.15f, 0.5f)
                .SetEase(Ease.InOutQuad)
                .SetLoops(-1, LoopType.Yoyo);
        }

        /// <summary>
        /// 脉冲动画重载（支持 GameObject 和自定义参数）
        /// </summary>
        public static Tween Pulse(GameObject go, float duration = 0.5f, int loops = -1)
        {
            if (go == null) return null;
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) return null;
            return rt
                .DOScale(1.15f, duration)
                .SetEase(Ease.InOutQuad)
                .SetLoops(loops, LoopType.Yoyo);
        }

        /// <summary>
        /// 按钮点击缩放动画（按下 → 小 → 回弹）
        /// </summary>
        public static Tween ButtonClickScale(Button button)
        {
            if (button == null) return null;
            var rt = button.GetComponent<RectTransform>();
            if (rt == null) return null;

            rt.localScale = Vector3.one;
            return rt
                .DOScale(0.85f, 0.08f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    rt.DOScale(1.05f, 0.1f)
                        .SetEase(Ease.OutBack)
                        .OnComplete(() => rt.localScale = Vector3.one);
                });
        }

        /// <summary>
        /// 抖动动画（位置左右抖动）
        /// </summary>
        /// <param name="rectTransform">目标 RectTransform</param>
        /// <returns>Tween（需手动 Kill）</returns>
        public static Tween Shake(RectTransform rectTransform)
        {
            if (rectTransform == null) return null;

            return rectTransform
                .DOShakeAnchorPos(0.5f, strength: 5f, vibrato: 10, fadingOut: true)
                .SetLoops(-1, LoopType.Yoyo);
        }

        /// <summary>
        /// 轻微抖动一次（用于提示/警告）
        /// </summary>
        public static Tween ShakeOnce(RectTransform rectTransform, float duration = 0.3f)
        {
            if (rectTransform == null) return null;

            return rectTransform
                .DOShakeAnchorPos(duration, strength: 8f, vibrato: 6, fadingOut: true);
        }

        /// <summary>
        /// 缩放弹跳抖动（X轴左右抖动，常用于升级失败）
        /// </summary>
        public static Tween ShakeScale(RectTransform rectTransform)
        {
            if (rectTransform == null) return null;

            return rectTransform
                .DOShakeAnchorPos(0.3f, strength: new Vector2(10f, 5f), vibrato: 8, fadingOut: true)
                .OnComplete(() => rectTransform.localScale = Vector3.one);
        }

        /// <summary>
        /// 血条抖动（怪物受伤时）
        /// </summary>
        public static Tween HealthBarShake(RectTransform rectTransform)
        {
            if (rectTransform == null) return null;

            return rectTransform
                .DOShakeAnchorPos(0.15f, strength: new Vector2(5f, 3f), vibrato: 4, fadingOut: true);
        }

        /// <summary>
        /// 波次大字公告动画
        /// </summary>
        public static Tween WaveAnnounce(TextMeshProUGUI text, int wave, float duration = 1.5f)
        {
            if (text == null) return null;

            text.gameObject.SetActive(true);
            text.text = $"第 {wave} 波";
            text.fontSize = 24f;
            text.color = Color.white;

            var rt = text.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one * 0.5f;
                return rt
                    .DOScale(1.2f, 0.3f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() =>
                    {
                        rt.DOScale(1f, 0.2f).SetEase(Ease.InOutQuad);
                        text
                            .DOFade(0f, duration)
                            .SetEase(Ease.InQuad)
                            .SetDelay(0.8f)
                            .OnComplete(() => text.gameObject.SetActive(false));
                    });
            }

            return text
                .DOFade(0f, duration)
                .SetEase(Ease.InQuad)
                .SetDelay(0.8f);
        }

        #endregion

        #region Slide Extension (Top/Bottom)

        /// <summary>
        /// 向上滑出到顶部（滑出后隐藏）
        /// </summary>
        public static Tween SlideOutToTop(RectTransform rectTransform, float duration = 0.3f, Action onComplete = null)
        {
            if (rectTransform == null) return null;

            var tween = rectTransform
                .DOAnchorPosY(rectTransform.rect.height, duration)
                .SetEase(Ease.InBack);

            if (onComplete != null)
                tween.OnComplete(() => onComplete());
            else
                tween.OnComplete(() => rectTransform.gameObject.SetActive(false));

            return tween;
        }

        /// <summary>
        /// 从底部滑入
        /// </summary>
        public static Tween SlideInFromBottom(RectTransform rectTransform, float duration = 0.3f)
        {
            if (rectTransform == null) return null;

            float startY = -rectTransform.rect.height;
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, startY);
            rectTransform.gameObject.SetActive(true);

            return rectTransform
                .DOAnchorPosY(0f, duration)
                .SetEase(Ease.OutBack);
        }

        #endregion

        #region Pop In/Out (Scale-based)

        /// <summary>
        /// 缩放弹入（0 → 1.1 → 1，带弹簧感）
        /// </summary>
        public static Tween PopIn(RectTransform rectTransform, float duration = 0.3f)
        {
            if (rectTransform == null) return null;

            rectTransform.localScale = Vector3.zero;
            rectTransform.gameObject.SetActive(true);

            return rectTransform
                .DOScale(1f, duration)
                .SetEase(Ease.OutBack);
        }

        /// <summary>
        /// 缩放弹出（1 → 0，弹出后隐藏）
        /// </summary>
        public static Tween PopOut(RectTransform rectTransform, float duration = 0.2f, Action onComplete = null)
        {
            if (rectTransform == null) return null;

            var tween = rectTransform
                .DOScale(0f, duration)
                .SetEase(Ease.InBack);

            if (onComplete != null)
                tween.OnComplete(() =>
                {
                    rectTransform.gameObject.SetActive(false);
                    rectTransform.localScale = Vector3.one;
                    onComplete();
                });
            else
                tween.OnComplete(() =>
                {
                    rectTransform.gameObject.SetActive(false);
                    rectTransform.localScale = Vector3.one;
                });

            return tween;
        }

        #endregion

        #region Screen Shake

        /// <summary>
        /// 屏幕震动（Camera 抖动）
        /// </summary>
        public static Tween ScreenShake(Camera cam, float strength = 0.3f, float duration = 0.2f)
        {
            if (cam == null) return null;

            return cam.transform
                .DOShakePosition(duration, strength: strength, vibrato: 8, fadingOut: true)
                .OnComplete(() => cam.transform.localPosition = Vector3.zero);
        }

        #endregion

        #region Fade Animations

        /// <summary>
        /// 淡入（CanvasGroup Alpha 0 → 1）
        /// </summary>
        /// <param name="canvasGroup">目标 CanvasGroup</param>
        /// <param name="duration">动画时长（秒）</param>
        /// <returns>Tween 以便链式调用</returns>
        public static Tween FadeIn(CanvasGroup canvasGroup, float duration = DefaultDuration)
        {
            if (canvasGroup == null) return null;

            canvasGroup.alpha = 0f;
            canvasGroup.gameObject.SetActive(true);

            return canvasGroup
                .DOFade(1f, duration)
                .SetEase(Ease.Linear);
        }

        /// <summary>
        /// 淡出（CanvasGroup Alpha 1 → 0，结束后隐藏）
        /// </summary>
        /// <param name="canvasGroup">目标 CanvasGroup</param>
        /// <param name="duration">动画时长（秒）</param>
        /// <returns>Tween 以便链式调用</returns>
        public static Tween FadeOut(CanvasGroup canvasGroup, float duration = DefaultDuration)
        {
            if (canvasGroup == null) return null;

            var tween = canvasGroup
                .DOFade(0f, duration)
                .SetEase(Ease.Linear);

            tween.OnComplete(() => canvasGroup.gameObject.SetActive(false));
            return tween;
        }

        #endregion

        #region Convenience Overloads (GameObject)

        /// <summary>
        /// 滑入面板（自动获取/添加 RectTransform）
        /// </summary>
        public static Tween SlideInUp(GameObject go, float duration = DefaultDuration)
        {
            return go != null ? SlideInUp(go.GetComponent<RectTransform>(), duration) : null;
        }

        /// <summary>
        /// 滑出面板
        /// </summary>
        public static Tween SlideOutUp(GameObject go, float duration = DefaultDuration)
        {
            return go != null ? SlideOutUp(go.GetComponent<RectTransform>(), duration) : null;
        }

        /// <summary>
        /// 缩放弹入
        /// </summary>
        public static Tween ScaleIn(GameObject go, float duration = DefaultDuration)
        {
            return go != null ? ScaleIn(go.GetComponent<RectTransform>(), duration) : null;
        }

        /// <summary>
        /// 缩放弹出
        /// </summary>
        public static Tween ScaleOut(GameObject go, float duration = DefaultDuration)
        {
            return go != null ? ScaleOut(go.GetComponent<RectTransform>(), duration) : null;
        }

        /// <summary>
        /// 脉冲动画
        /// </summary>
        public static Tween Pulse(GameObject go)
        {
            return go != null ? Pulse(go.GetComponent<RectTransform>()) : null;
        }

        /// <summary>
        /// 抖动动画
        /// </summary>
        public static Tween Shake(GameObject go)
        {
            return go != null ? Shake(go.GetComponent<RectTransform>()) : null;
        }

        #endregion

        #region Stop Animations

        /// <summary>
        /// 停止脉冲动画（安全版本，传入 null 也不会报错）
        /// </summary>
        public static void StopPulse(RectTransform rectTransform)
        {
            if (rectTransform == null) return;
            rectTransform.DOKill();
            rectTransform.localScale = Vector3.one;
        }

        /// <summary>
        /// 停止脉冲动画（GameObject 版本）
        /// </summary>
        public static void StopPulse(GameObject go)
        {
            if (go == null) return;
            StopPulse(go.GetComponent<RectTransform>());
        }

        /// <summary>
        /// 停止抖动动画（安全版本）
        /// </summary>
        public static void StopShake(RectTransform rectTransform)
        {
            if (rectTransform == null) return;
            rectTransform.DOKill();
            rectTransform.localPosition = Vector3.zero;
            rectTransform.localScale = Vector3.one;
        }

        /// <summary>
        /// 停止抖动动画（GameObject 版本）
        /// </summary>
        public static void StopShake(GameObject go)
        {
            if (go == null) return;
            StopShake(go.GetComponent<RectTransform>());
        }

        /// <summary>
        /// 停止所有动画并重置 Transform
        /// </summary>
        public static void StopAll(RectTransform rectTransform)
        {
            if (rectTransform == null) return;
            rectTransform.DOKill();
            rectTransform.localPosition = Vector3.zero;
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// 停止所有动画（GameObject 版本）
        /// </summary>
        public static void StopAll(GameObject go)
        {
            if (go == null) return;
            StopAll(go.GetComponent<RectTransform>());
        }

        /// <summary>
        /// 批量停止指定 Id 的所有 Tween
        /// </summary>
        public static void KillById(string tweenId)
        {
            DG.Tweening.Tween.Kill(tweenId);
        }

        /// <summary>
        /// 停止指定对象上的所有动画
        /// </summary>
        public static void KillTarget(object target)
        {
            DG.Tweening.Tween.Kill(target);
        }

        #endregion
    }
}
