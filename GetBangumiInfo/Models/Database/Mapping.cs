using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GetBangumiInfo.Models.Database;

[Table("IdMapping")]
public class Mapping
{
    [Key]
    [Column("bangumiSubjectId")]
    public int BangumiId { get; set; }

    [Column("bilibiliSeasonId")]
    public int BilibiliId { get; set; }

    [Column("dandanId")]
    public int DandanId { get; set; }

    [Column("dandanId")]
    public DateTimeOffset? AirDate { get; set; }

    [Column("isJapaneseAnime")]
    public bool? IsJapaneseAnime { get; set; }
}
