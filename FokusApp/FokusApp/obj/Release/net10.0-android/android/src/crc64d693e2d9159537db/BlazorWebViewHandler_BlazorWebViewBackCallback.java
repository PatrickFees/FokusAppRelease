package crc64d693e2d9159537db;


public class BlazorWebViewHandler_BlazorWebViewBackCallback
	extends androidx.activity.OnBackPressedCallback
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_handleOnBackPressed:()V:GetHandleOnBackPressedHandler\n" +
			"";
		mono.android.Runtime.register ("Microsoft.AspNetCore.Components.WebView.Maui.BlazorWebViewHandler+BlazorWebViewBackCallback, Microsoft.AspNetCore.Components.WebView.Maui", BlazorWebViewHandler_BlazorWebViewBackCallback.class, __md_methods);
	}

	public BlazorWebViewHandler_BlazorWebViewBackCallback (boolean p0)
	{
		super (p0);
		if (getClass () == BlazorWebViewHandler_BlazorWebViewBackCallback.class) {
			mono.android.TypeManager.Activate ("Microsoft.AspNetCore.Components.WebView.Maui.BlazorWebViewHandler+BlazorWebViewBackCallback, Microsoft.AspNetCore.Components.WebView.Maui", "System.Boolean, System.Private.CoreLib", this, new java.lang.Object[] { p0 });
		}
	}

	public void handleOnBackPressed ()
	{
		n_handleOnBackPressed ();
	}

	private native void n_handleOnBackPressed ();

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
