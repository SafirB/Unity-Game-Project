using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GuardFOV))]
public class FOVEditor : Editor
{
    private void OnSceneGUI()
    {
        GuardFOV fov = (GuardFOV)target;
        Handles.color = Color.white;
        // Radius
        Handles.DrawWireArc(fov.transform.position, Vector3.up, Vector3.forward, 360, fov.radius);

        Vector3 viewAngle01 = AngleDirection(fov.transform.eulerAngles.y, -fov.angle / 2);
        Vector3 viewAngle02 = AngleDirection(fov.transform.eulerAngles.y, fov.angle / 2);

        // Outline of FOV cone
        Handles.color = Color.yellow;
        Handles.DrawLine(fov.transform.position, fov.transform.position+ viewAngle01 * fov.radius);
        Handles.DrawLine(fov.transform.position, fov.transform.position + viewAngle02 * fov.radius);

        // Makes a red line when the players is inside FOV
        if (fov.canSeePlayer)
        {
            Handles.color = Color.red;
            Handles.DrawLine(fov.transform.position, fov.playerRef.transform.position);
        }
    }

    private Vector3 AngleDirection(float eulerY, float angleInDegrees)
    {
        angleInDegrees += eulerY;
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}
