using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
public class Mirror : MonoBehaviour
{
    [Header("Settings")]
    public int resolution = 2048;
    public string shaderName = "Universal Render Pipeline/Lit";

    private RenderTexture mirror_render_texture;
    private Material mirror_material;
    private Camera camera;

    void Start()
    {
        camera = GetComponent<Camera>();
        camera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Base;

        mirror_render_texture = new RenderTexture(resolution, resolution, 12);
        mirror_render_texture.name = "Mirror_RenderTexture_" + GetInstanceID();

        camera.targetTexture = mirror_render_texture;

        mirror_material = new Material(Shader.Find(shaderName));
        mirror_material.name = "Mirror_Material";
        
        if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
            mirror_material.SetTexture("_BaseMap", mirror_render_texture);
        else
            mirror_material.SetTexture("_MainTex", mirror_render_texture);

        transform.parent.GetComponent<Renderer>().material = mirror_material;
    }

    private void OnDestroy()
    {
        // Clean up to avoid memory leaks in the editor
        if (mirror_render_texture != null) mirror_render_texture.Release();
        if (mirror_material != null) Destroy(mirror_material);
    }
}