namespace LetMePlayBBPlus
{
    public class LightManager
    {
        private EnvironmentController ec;
        private bool lightsWereOn;

        public LightManager(EnvironmentController ec)
        {
            this.ec = ec;
        }

        public void DisableAllLights()
        {
            lightsWereOn = ec.lights.Count > 0 && ec.lights.Exists(cell => cell.lightOn);

            ec.SetAllLights(false);
        }

        public void RestoreLights()
        {
            if (lightsWereOn)
            {
                ec.SetAllLights(true);
            }
        }
    }
}