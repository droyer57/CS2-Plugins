using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Revive;

public static class Helpers
{
    public static CPointWorldText CreateText(Vector position, string text, int size = 150)
    {
        var worldText = Utilities.CreateEntityByName<CPointWorldText>("point_worldtext");
        if (worldText == null || !worldText.IsValid)
        {
            return null!;
        }

        var color = Color.White;

        worldText.MessageText = text;
        worldText.Enabled = true;
        worldText.FontSize = size;
        worldText.Color = color;
        worldText.Fullbright = true;
        worldText.WorldUnitsPerPx = 0.1f;
        worldText.DepthOffset = 0.0f;
        // worldText.DepthOffset = -1.0f;
        worldText.JustifyHorizontal = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_CENTER;
        worldText.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_CENTER;
        worldText.ReorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_AROUND_UP;
        worldText.DispatchSpawn();
        worldText.Teleport(position, new QAngle(0.0f, -180.0f, 90.0f));

        return worldText;
    }

    public static CDynamicProp CreateProp(Vector position)
    {
        var prop = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (prop == null || !prop.IsValid)
            return null!;

        prop.SetModel("models/coop/challenge_coin.vmdl");
        prop.DispatchSpawn();
        prop.Teleport(position, new QAngle(0, 0, 0), Vector.Zero);

        prop.Glow.GlowColorOverride = Color.Red;
        prop.Glow.GlowRange = 5000;
        prop.Glow.GlowRangeMin = 0;
        prop.Glow.GlowTeam = -1; // -1 = everyone
        prop.Glow.GlowType = 3; // 3 = visible through walls

        prop.CBodyComponent!.SceneNode!.Scale = 0.5f;
        return prop;
    }

    public static CBeam[] DrawBeaconCircle(Vector position)
    {
        var lines = 20;
        var beamEnt = new CBeam[lines];

        // draw piecewise approx by stepping angle
        // and joining points with a dot to dot
        var step = (float)(2.0f * Math.PI) / lines;
        var radius = 50;

        var angleOld = 0.0f;
        var angleCur = step;

        for (var i = 0; i < lines; i++) // Drawing Beacon Circle
        {
            var start = AngleOnCircle(angleOld, radius, position);
            var end = AngleOnCircle(angleCur, radius, position);

            beamEnt[i] = DrawLaserBetween(start, end, Color.Red, 2.0f);

            angleOld = angleCur;
            angleCur += step;
        }

        return beamEnt;
    }

    private static CBeam DrawLaserBetween(Vector startPos, Vector endPos, Color color, float width)
    {
        var beam = Utilities.CreateEntityByName<CBeam>("beam");

        if (beam == null || !beam.IsValid)
            return null!;

        beam.Render = color;
        beam.Width = width;

        beam.Teleport(startPos, QAngle.Zero, Vector.Zero);
        beam.EndPos.X = endPos.X;
        beam.EndPos.Y = endPos.Y;
        beam.EndPos.Z = endPos.Z;
        beam.DispatchSpawn();

        return beam;
    }

    private static Vector AngleOnCircle(float angle, float radius, Vector mid)
    {
        // {r * cos(x),r * sin(x)} + mid
        // NOTE: we offset Z so it doesn't clip into the ground
        return new Vector((float)(mid.X + radius * Math.Cos(angle)), (float)(mid.Y + radius * Math.Sin(angle)),
            mid.Z + 6.0f);
    }

    public static float CalculateDistanceBetween(Vector point1, Vector point2)
    {
        var dx = point2.X - point1.X;
        var dy = point2.Y - point1.Y;
        var dz = point2.Z - point1.Z;

        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}