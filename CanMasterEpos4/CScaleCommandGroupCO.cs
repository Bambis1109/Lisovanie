namespace EposCmd.Net
{
    public class CScaleCommandGroupCO : CCommandGroupCO
    {
        // Typovaný prístup k dátam váhy/STM32
        protected CDataScale Data => (CDataScale)BaseData;
    }
}