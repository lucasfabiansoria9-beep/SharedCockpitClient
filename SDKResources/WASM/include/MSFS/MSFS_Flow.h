//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation. All Rights Reserved.
//
//-----------------------------------------------------------------------------

#ifndef _MSFS_FLOW_H
#define _MSFS_FLOW_H

enum FsFlowEvent : unsigned short
{
	FsFlowEvent_None,
	FsFlowEvent_FltLoad,
	FsFlowEvent_FltLoaded,
	FsFlowEvent_TeleportStart,
	FsFlowEvent_TeleportDone,
	FsFlowEvent_BackOnTrackStart,
	FsFlowEvent_BackOnTrackDone,
	FsFlowEvent_SkipStart,
	FsFlowEvent_SkipDone,
	FsFlowEvent_BackToMainMenu,
	FsFlowEvent_RTCStart,
	FsFlowEvent_RTCEnd,
	FsFlowEvent_ReplayStart,
	FsFlowEvent_ReplayEnd,
	FsFlowEvent_FlightStart,
	FsFlowEvent_FlightEnd,
	FsFlowEvent_PlaneCrash,
};

typedef void (*fsFlowWasmCallback)(FsFlowEvent event, const char* buf, unsigned int bufSize, void* ctx);

#ifdef __cplusplus
extern "C" {
#endif

    bool fsFlowRegister(fsFlowWasmCallback callback, void* context = nullptr);

	bool fsFlowUnregister(fsFlowWasmCallback callback = nullptr);

	bool fsFlowUnregisterAll();

#ifdef __cplusplus
}
#endif


#endif //!_MSFS_FLOW_H