
using AutoMapper;
using dsstats.shared;
namespace dsstats.db;

public class DsstatsAutoMapperProfile : Profile
{
    public DsstatsAutoMapperProfile()
    {
        CreateMap<Player, PlayerDto>(MemberList.Destination);
        CreateMap<PlayerDto, Player>(MemberList.Source);

        CreateMap<Replay, ReplayDto>(MemberList.Destination)
            .ForMember(s => s.FileName, opt => opt.Ignore())
            .ForMember(s => s.PlayerResult, opt => opt.Ignore())
            .ForMember(s => s.PlayerPos, opt => opt.Ignore())
            .ForMember(s => s.ResultCorrected, opt => opt.Ignore())
            .ForMember(s => s.Objective, opt => opt.Ignore())
            .ForMember(s => s.DefaultFilter, opt => opt.Ignore())
            .ForMember(s => s.Downloads, opt => opt.Ignore())
            .ForMember(s => s.ReplayEvent, opt => opt.Ignore())
            .ForMember(m => m.TournamentEdition, opt => opt.MapFrom(m => m.IsTE))
            .ForMember(m => m.Middle, opt => opt.Ignore())
            .AfterMap((src, dest) =>
            {
                var middle = src.MiddleControlData;
                if (middle is not null)
                {
                    dest = dest with { Middle = middle.FirstTeam + "|" + string.Join('|', middle.Gameloops) };
                }
            });
        CreateMap<ReplayDto, Replay>(MemberList.Source)
            .ForSourceMember(s => s.FileName, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.PlayerResult, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.PlayerPos, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.ResultCorrected, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.Objective, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.DefaultFilter, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.Downloads, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.ReplayEvent, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.Middle, opt => opt.DoNotValidate())
            .ForMember(m => m.IsTE, opt => opt.MapFrom(m => m.TournamentEdition))
            .AfterMap((src, dest) =>
            {
                if (!string.IsNullOrEmpty(src.Middle))
                {
                    var parts = src.Middle.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0 && int.TryParse(parts[0], out int firstTeam))
                    {
                        var loops = parts.Skip(1)
                                        .Select(str => int.TryParse(str, out var val) ? val : 0)
                                        .ToList();

                        dest.MiddleControlData = new MiddleControl(firstTeam, loops);
                    }
                }
            });

        CreateMap<ReplayPlayer, ReplayPlayerDto>(MemberList.Destination)
            .ForMember(x => x.MmrChange, opt => opt.Ignore())
            .ForMember(x => x.Downloads, opt => opt.Ignore())
            .ForMember(x => x.Views, opt => opt.Ignore())
            .ForMember(x => x.OppRace, opt => opt.Ignore())
            .ForMember(x => x.Upgrades, opt => opt.MapFrom(m => m.PlayerUpgrades));

        CreateMap<ReplayPlayerDto, ReplayPlayer>(MemberList.Source)
            .ForSourceMember(x => x.MmrChange, opt => opt.DoNotValidate())
            .ForSourceMember(x => x.Downloads, opt => opt.DoNotValidate())
            .ForSourceMember(x => x.Views, opt => opt.DoNotValidate())
            .ForSourceMember(x => x.OppRace, opt => opt.DoNotValidate())
            .ForMember(m => m.PlayerUpgrades, opt => opt.MapFrom(m => m.Upgrades));

        CreateMap<Spawn, SpawnDto>(MemberList.Destination)
            .ForMember(m => m.Units, opt => opt.MapFrom(m => m.SpawnUnits));
        CreateMap<SpawnDto, Spawn>(MemberList.Source)
            .ForMember(m => m.SpawnUnits, opt => opt.MapFrom(m => m.Units));

        CreateMap<SpawnUnit, SpawnUnitDto>(MemberList.Destination)
            .ForMember(dest => dest.Poss,
                opt => opt.MapFrom(src =>
                string.Join(",", src.Positions.SelectMany(p => new[] { p.X, p.Y }))));
        CreateMap<SpawnUnitDto, SpawnUnit>(MemberList.Source)
            .ForMember(dest => dest.Positions,
                opt => opt.MapFrom(src =>
                    src.Poss.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Chunk(2)
                        .Select(pair => new Position(int.Parse(pair[0]), int.Parse(pair[1])))
                        .ToList()
                    ))
            .ForSourceMember(src => src.Poss, opt => opt.DoNotValidate());

        CreateMap<PlayerUpgrade, PlayerUpgradeDto>(MemberList.Destination);
        CreateMap<PlayerUpgradeDto, PlayerUpgrade>(MemberList.Source);

        CreateMap<Unit, UnitDto>(MemberList.Destination);
        CreateMap<UnitDto, Unit>(MemberList.Source);

        CreateMap<Upgrade, UpgradeDto>(MemberList.Destination);
        CreateMap<UpgradeDto, Upgrade>(MemberList.Source);

        // ng
        CreateMap<PlayerRating, dsstats.shared8.PlayerRatingDto>(MemberList.Destination);
    }
}