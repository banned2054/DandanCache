using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GetBangumiInfo.Models.Database;

[Table("IdMapping")]
public class Mapping
{
    [Key]
    [Column("bangumi_subject_id")]
    public int BangumiId { get; set; }

    [Column("bilibili_season_id")]
    public int BilibiliId { get; set; }

    [Column("dandan_id")]
    public float? DandanId { get; set; }
}
