---
description: The return value of the SetEffect() API
---

# IAutoResetWaitable

| NameSpace    | Accessibility |
| ------------ | ------------- |
| Ami.BroAudio | public        |

## Public Methods

<table data-full-width="false"><thead><tr><th width="173">Method</th><th width="155">Return</th><th width="150">Parameters</th><th width="271">Description</th></tr></thead><tbody><tr><td><mark style="color:orange;"><strong>Until</strong></mark></td><td><a href="https://docs.unity3d.com/ScriptReference/WaitUntil.html">WaitUntil</a></td><td><mark style="color:green;">Func&#x3C;bool></mark> condition</td><td>Wait until the given condition is met</td></tr><tr><td><mark style="color:orange;"><strong>While</strong></mark></td><td><a href="https://docs.unity3d.com/ScriptReference/WaitWhile.html">WaitWhile</a></td><td><mark style="color:green;">Func&#x3C;bool></mark> condition</td><td>Wait while the given condition is true</td></tr><tr><td><mark style="color:orange;"><strong>ForSeconds</strong></mark></td><td><a href="https://docs.unity3d.com/ScriptReference/WaitForSeconds.html">WaitForSeconds</a></td><td><mark style="color:green;">float</mark> seconds</td><td>Wait by the given time</td></tr></tbody></table>

🔔**This can be used as a yield return in Coroutine,** for example:

<pre class="language-csharp"><code class="lang-csharp"><strong>private bool _isUnderWater = false;
</strong><strong>
</strong><strong>public void EnterWater(bool isEnter)
</strong>{
    _isUnderWater = isEnter;
    if(isEnter)
    {
        StartCoroutine(SetUnderWaterEffect());
    }
}

private IEnumerator SetUnderWaterEffect()
{
    yield return BroAudio.SetEffect(Effect.LowPass())
                        .WaitWhile(() => _isUnderWater = true);
}
</code></pre>

