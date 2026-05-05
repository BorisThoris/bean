using Sandbox;

namespace Foliage;


public interface IFoliagePigment
{
	/// <summary>
	/// Object settings for the pigment.
	/// </summary>
	public FoliageObjectSettings Settings { get; }

	/// <summary>
	/// Placement settings for the pigment.
	/// </summary>
	public FoliagePlacementSettings Placement { get; }

	/// <summary>
	/// Called when the pigment is loaded.
	/// Used for setting up the pigment if needed.
	/// </summary>
	public void OnLoad() { }

	/// <summary>
	/// The radius of the piece of foliage that this pigment paints.
	/// </summary>
	public float Radius();

	/// <summary>
	/// Called when the pigment is painted, should return a new foliage object that was painted.
	/// </summary>
	public GameObject? Paint( Scene scene );


}
