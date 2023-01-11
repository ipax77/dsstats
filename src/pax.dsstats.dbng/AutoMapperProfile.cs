﻿using AutoMapper;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using pax.dsstats.shared;

namespace pax.dsstats.dbng
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // dotnet 7 workaround
            // IncludeSourceExtensionMethods(typeof(SourceExtensions));
            // ShouldMapMethod = (m => m.Name != nameof(SourceExtensions.AnotherNumber));

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

            CreateMap<Replay, ReplayListDto>(MemberList.Destination)
                .ForMember(x => x.Commander, opt => opt.Ignore())
                .ForMember(x => x.MmrChange, opt => opt.Ignore())
                .ForMember(x => x.Cmdrs1, opt => opt.Ignore())
                .ForMember(x => x.Cmdrs2, opt => opt.Ignore());
            CreateMap<Replay, ReplayListEventDto>(MemberList.Destination)
                .ForMember(x => x.Commander, opt => opt.Ignore())
                .ForMember(x => x.MmrChange, opt => opt.Ignore())
                .ForMember(x => x.Cmdrs1, opt => opt.Ignore())
                .ForMember(x => x.Cmdrs2, opt => opt.Ignore());
            CreateMap<ReplayEvent, ReplayEventListDto>(MemberList.Destination);

            CreateMap<Replay, ReplayDsRDto>(MemberList.Destination);
            CreateMap<ReplayDto, ReplayDsRDto>(MemberList.Destination);

            CreateMap<ReplayPlayer, ReplayPlayerDsRDto>(MemberList.Destination);
            CreateMap<ReplayPlayerDto, ReplayPlayerDsRDto>(MemberList.Destination);

            CreateMap<Player, PlayerDsRDto>(MemberList.Destination);
            CreateMap<PlayerDto, PlayerDsRDto>(MemberList.Destination);

            CreateMap<Player, PlayerRatingDto>(MemberList.Destination);
            CreateMap<Player, PlayerMapDto>(MemberList.Destination);

            CreateMap<UploaderDto, Uploader>(MemberList.Source);
            CreateMap<BattleNetInfoDto, BattleNetInfo>(MemberList.Source)
                .ForSourceMember(x => x.PlayerUploadDtos, opt => opt.DoNotValidate());
            CreateMap<PlayerUploadDto, Player>(MemberList.Source);

            CreateMap<PlayerRating, PlayerRatingDto>(MemberList.Destination);
            CreateMap<Player, PlayerRatingPlayerDto>(MemberList.Destination);
            CreateMap<PlayerRating, PlayerRatingDetailDto>(MemberList.Destination)
                .ForMember(x => x.FakeDiff, opt => opt.Ignore())
                .ForMember(x => x.MmrChange, opt => opt.Ignore());
            CreateMap<PlayerRating, PlayerRatingInfoDto>(MemberList.Destination);

            CreateMap<NoUploadResult, NoUploadResult>()
                .ForMember(x => x.NoUploadResultId, opt => opt.Ignore())
                .ForMember(x => x.Player, opt => opt.Ignore())
                .ForSourceMember(x => x.NoUploadResultId, opt => opt.DoNotValidate())
                .ForSourceMember(x => x.Player, opt => opt.DoNotValidate());
        }
    }
}
