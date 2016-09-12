package md5bbfa0ee849683f03e23f9abdeac5453a;


public abstract class UserInteractionHandler
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"";
		mono.android.Runtime.register ("Esri.ArcGISRuntime.UI.UserInteractionHandler, Esri.ArcGISRuntime, Version=100.0.0.0, Culture=neutral, PublicKeyToken=29c6dd6e8553d944", UserInteractionHandler.class, __md_methods);
	}


	public UserInteractionHandler () throws java.lang.Throwable
	{
		super ();
		if (getClass () == UserInteractionHandler.class)
			mono.android.TypeManager.Activate ("Esri.ArcGISRuntime.UI.UserInteractionHandler, Esri.ArcGISRuntime, Version=100.0.0.0, Culture=neutral, PublicKeyToken=29c6dd6e8553d944", "", this, new java.lang.Object[] {  });
	}

	public UserInteractionHandler (md5bbfa0ee849683f03e23f9abdeac5453a.GeoView p0) throws java.lang.Throwable
	{
		super ();
		if (getClass () == UserInteractionHandler.class)
			mono.android.TypeManager.Activate ("Esri.ArcGISRuntime.UI.UserInteractionHandler, Esri.ArcGISRuntime, Version=100.0.0.0, Culture=neutral, PublicKeyToken=29c6dd6e8553d944", "Esri.ArcGISRuntime.UI.GeoView, Esri.ArcGISRuntime, Version=100.0.0.0, Culture=neutral, PublicKeyToken=29c6dd6e8553d944", this, new java.lang.Object[] { p0 });
	}

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
