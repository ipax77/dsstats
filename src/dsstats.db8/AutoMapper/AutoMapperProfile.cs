using AutoMapper;
using dsstats.shared;

namespace dsstats.db8.AutoMapper;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<Player, PlayerDto>(MemberList.Destination);
        CreateMap<PlayerDto, Player>(MemberList.Source);

        CreateMap<Replay, ReplayDto>(MemberList.Destination);
        CreateMap<ReplayDto, Replay>(MemberList.Source);

        CreateMap<ReplayPlayer, ReplayPlayerDto>(MemberList.Destination)
            .ForMember(x => x.MmrChange, opt => opt.Ignore());
        CreateMap<ReplayPlayerDto, ReplayPlayer>(MemberList.Source)
            .ForSourceMember(x => x.MmrChange, opt => opt.DoNotValidate());

        CreateMap<Spawn, SpawnDto>(MemberList.Destination);
        CreateMap<SpawnDto, Spawn>(MemberList.Source);

        CreateMap<SpawnUnit, SpawnUnitDto>(MemberList.Destination);
        CreateMap<SpawnUnitDto, SpawnUnit>(MemberList.Source);

        CreateMap<PlayerUpgrade, PlayerUpgradeDto>(MemberList.Destination);
        CreateMap<PlayerUpgradeDto, PlayerUpgrade>(MemberList.Source);

        CreateMap<Unit, UnitDto>(MemberList.Destination);
        CreateMap<UnitDto, Unit>(MemberList.Source);

        CreateMap<Upgrade, UpgradeDto>(MemberList.Destination);
        CreateMap<UpgradeDto, Upgrade>(MemberList.Source);

        CreateMap<ReplayEvent, ReplayEventDto>(MemberList.Destination);
        CreateMap<ReplayEventDto, ReplayEvent>(MemberList.Source);

        CreateMap<EventDto, Event>(MemberList.Source);
        CreateMap<Event, EventDto>(MemberList.Destination);
        CreateMap<Event, EventListDto>(MemberList.Destination);

        CreateMap<PlayerRating, PlayerRatingDto>(MemberList.Destination);
        CreateMap<PlayerRatingChange, PlayerRatingChangeDto>(MemberList.Destination);

        CreateMap<Player, PlayerRatingPlayerDto>(MemberList.Destination)
            .ForMember(x => x.IsUploader, opt => opt.Ignore());
        CreateMap<PlayerRating, PlayerRatingDetailDto>(MemberList.Destination)
            .ForMember(x => x.FakeDiff, opt => opt.Ignore())
            .ForMember(x => x.MmrChange, opt => opt.Ignore());

        // Arcade
        CreateMap<ArcadePlayerRating, ArcadePlayerRatingDto>(MemberList.Destination);
        CreateMap<ArcadePlayer, ArcadePlayerRatingPlayerDto>(MemberList.Destination);
        CreateMap<ArcadePlayerRatingChange, ArcadePlayerRatingChangeDto>(MemberList.Destination);

        CreateMap<ArcadePlayerRating, ArcadePlayerRatingDetailDto>(MemberList.Destination);
        CreateMap<ArcadePlayer, ArcadePlayerDto>(MemberList.Destination);
        CreateMap<ArcadeReplayDsPlayerRating, ArcadeReplayPlayerRatingDto>(MemberList.Destination);
        CreateMap<ArcadeReplayRating, ArcadeReplayRatingDto>(MemberList.Destination);
        CreateMap<ArcadeReplayPlayer, ArcadeReplayPlayerDto>(MemberList.Destination);
        CreateMap<ArcadeReplay, ArcadeReplayDto>(MemberList.Destination);
        CreateMap<ArcadeReplay, ArcadeReplayListDto>(MemberList.Destination)
            .ForMember(x => x.MmrChange, opt => opt.Ignore());

        CreateMap<ArcadeReplay, ArcadeReplayListRatingDto>(MemberList.Destination)
            .ForMember(x => x.MmrChange, opt => opt.Ignore());
        CreateMap<ArcadeReplayDsPlayer, ArcadeReplayPlayerListDto>(MemberList.Destination);
        CreateMap<ArcadePlayer, ArcadePlayerListDto>(MemberList.Destination);
        CreateMap<ArcadeReplayRating, ArcadeReplayRatingListDto>(MemberList.Destination);
        CreateMap<ArcadeReplayDsPlayerRating, ArcadeReplayPlayerRatingListDto>(MemberList.Destination);

        CreateMap<ArcadePlayerRating, PlayerRatingDto>(MemberList.Destination)
            .ForMember(x => x.Player, opt => opt.MapFrom(m => m.Player))
            .ForMember(x => x.PlayerRatingChange, opt => opt.MapFrom(m => m.ArcadePlayerRatingChange));
        CreateMap<ArcadePlayer, PlayerRatingPlayerDto>(MemberList.Destination)
            .ForMember(x => x.ToonId, opt => opt.MapFrom(m => m.ProfileId))
            .ForMember(x => x.ArcadeDefeatsSinceLastUpload, opt => opt.Ignore())
            .ForMember(x => x.IsUploader, opt => opt.Ignore());
        CreateMap<ArcadePlayerRatingChange, PlayerRatingChangeDto>();
        CreateMap<ArcadePlayer, ArcadePlayerReplayDto>(MemberList.Destination);
        CreateMap<ArcadeReplayDsPlayer, ArcadeReplayDsPlayerDto>(MemberList.Destination);
        CreateMap<ArcadeReplayDsPlayerDto, ArcadeReplayDsPlayer>(MemberList.Source);

        CreateMap<DsUnit, DsUnitDto>(MemberList.Destination);
        CreateMap<DsUnitDto, DsUnit>(MemberList.Source);
        CreateMap<DsWeapon, DsWeaponDto>(MemberList.Destination);
        CreateMap<DsWeaponDto, DsWeapon>(MemberList.Source);
        CreateMap<BonusDamage, BonusDamageDto>(MemberList.Destination);
        CreateMap<BonusDamageDto,BonusDamage>(MemberList.Source);
        CreateMap<DsAbility, DsAbilityDto>(MemberList.Destination);
        CreateMap<DsAbilityDto, DsAbility>(MemberList.Source);
        CreateMap<DsUpgrade, DsUpgradeDto>(MemberList.Destination);
        CreateMap<DsUpgradeDto, DsUpgrade>(MemberList.Source);

        CreateMap<DsUnit, DsUnitListDto>(MemberList.Destination);
        CreateMap<DsUnit, DsUnitBuildDto>(MemberList.Destination);

        CreateMap<Faq, FaqDto>(MemberList.Destination);
        CreateMap<FaqDto, Faq>(MemberList.Source)
            .ForSourceMember(s => s.FaqId, opt => opt.DoNotValidate());

        CreateMap<IhSession, IhSessionListDto>(MemberList.Destination);
        CreateMap<IhSession, IhSessionDto>(MemberList.Destination);
        CreateMap<IhSessionPlayer, IhSessionPlayerDto>(MemberList.Destination);
    }
}