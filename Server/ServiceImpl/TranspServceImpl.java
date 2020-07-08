package bg.diplNS.service.impl;



import java.time.LocalDateTime;

import java.util.ArrayList;

import java.util.Arrays;

import java.util.HashMap;

import java.util.HashSet;

import java.util.LinkedList;

import java.util.List;

import java.util.Map;

import java.util.NoSuchElementException;

import java.util.Objects;

import java.util.Set;

import java.util.concurrent.Executors;

import java.util.concurrent.ScheduledExecutorService;

import java.util.concurrent.TimeUnit;



import org.apache.commons.lang3.ArrayUtils;

import org.springframework.beans.factory.annotation.Autowired;

import org.springframework.context.ApplicationEventPublisher;

import org.springframework.stereotype.Component;



import com.google.common.base.Strings;



import bg.diplNS.dto.LeDetailsDtoProd;

import bg.diplNS.dto.LeDetailsPickingDTO;

import bg.diplNS.dto.SeparetedLeDTO;

import bg.diplNS.dto.ShipmentTransportOrderDTO;

import bg.diplNS.dto.StartedTransportsDto;

import bg.diplNS.dto.TransferZonesDTO;

import bg.diplNS.dto.TranspLoadingCountDTO;

import bg.diplNS.dto.TransportOrderDTO;

import bg.diplNS.error.BackendError;

import bg.diplNS.events.FinishLeEvent;

import bg.diplNS.events.SendAllocationEvent;

import bg.diplNS.graphAlgorithms.DijkstraImpl;

import bg.diplNS.graphAlgorithms.Edge;

import bg.diplNS.graphAlgorithms.Graph;

import bg.diplNS.helper.LagerortWithPosition;

import bg.diplNS.helper.LocalDateTimeProvider;

import bg.diplNS.model.tables.Art;

import bg.diplNS.model.tables.ArtUnitOfMeasure;

import bg.diplNS.model.tables.Best;

import bg.diplNS.model.tables.BinType;

import bg.diplNS.model.tables.Configuration;

import bg.diplNS.model.tables.Lagerort;

import bg.diplNS.model.tables.Lber;

import bg.diplNS.model.tables.Le;

import bg.diplNS.model.tables.Letyp;

import bg.diplNS.model.tables.Lzone;

import bg.diplNS.model.tables.LzoneTranspRoute;

import bg.diplNS.model.tables.Mandant;

import bg.diplNS.model.tables.Nk;

import bg.diplNS.model.tables.PickingSrcLe;

import bg.diplNS.model.tables.ReceiptLe;

import bg.diplNS.model.tables.SalesHeader;

import bg.diplNS.model.tables.ShipmentHeader;

import bg.diplNS.model.tables.ShipmentLe;

import bg.diplNS.model.tables.ShipmentToSalesHeaderAssoc;

import bg.diplNS.model.tables.Transp;

import bg.diplNS.model.tables.TranspJournal;

import bg.diplNS.model.tables.enums.EventType;

import bg.diplNS.model.tables.enums.JournalStatus;

import bg.diplNS.model.tables.enums.LeStatus;

import bg.diplNS.model.tables.enums.LogisticType;

import bg.diplNS.model.tables.enums.PickingSrcLeStatus;

import bg.diplNS.model.tables.enums.ProcessType;

import bg.diplNS.model.tables.enums.SalesHeaderStatus;

import bg.diplNS.model.tables.enums.ShipmentHeaderStatus;

import bg.diplNS.model.tables.enums.ShipmentLeStatus;

import bg.diplNS.model.tables.enums.TransportStatus;

import bg.diplNS.remote.helper.Assert;

import bg.diplNS.remote.helper.ObjectHelper;

import bg.diplNS.repository.TranspRepository;

import bg.diplNS.service.ArtUnitOfMeasureService;

import bg.diplNS.service.BestService;

import bg.diplNS.service.BinTypeService;

import bg.diplNS.service.ConfigurationService;

import bg.diplNS.service.LagerortService;

import bg.diplNS.service.LberService;

import bg.diplNS.service.LeService;

import bg.diplNS.service.LetypService;

import bg.diplNS.service.LzoneTranspRouteService;

import bg.diplNS.service.MandantService;

import bg.diplNS.service.NkService;

import bg.diplNS.service.PickingSrcLeService;

import bg.diplNS.service.SalesHeaderService;

import bg.diplNS.service.ShipmentHeaderService;

import bg.diplNS.service.ShipmentLeService;

import bg.diplNS.service.ShipmentToSalesHeaderAssocService;

import bg.diplNS.service.TranspJournalService;

import bg.diplNS.service.TranspService;

import lombok.extern.slf4j.Slf4j;


@Slf4j

@Component

public class TranspServiceImpl<T extends Transp> implements TranspService<T> {



	TranspRepository<T> transpRepository;

	@Autowired

	LeService<Le> leService;

	@Autowired



	public TranspServiceImpl(TranspRepository<T> transpRepository) {

		this.transpRepository = transpRepository;

	}



	@Override

	public T findById(Long id) {

		return transpRepository.findById(id).orElseThrow(NoSuchElementException::new);

	}



	@Override

	public List<T> findByZiel(long zielId) {

		return transpRepository.findByZiel(zielId);

	}



	@Override

	public void delete(T entity) {

		createTranspJournal(entity, JournalStatus.DELETED, null);

		transpRepository.delete(entity);

	}





	@Override

	public T update(T entity, EventType eventType) {

		createTranspJournal(entity, JournalStatus.MODIFIED, eventType);

		return transpRepository.save(entity);

	}



	@Override



	public T create(T entity) {

		return transpRepository.save(entity);

	}



	public static int intValue(Number num, int nullValue) {

		return (num != null) ? num.intValue() : nullValue;

	}




	@Override

	public T create(Class<T> transportClass, Le le, LagerortWithPosition ziel, Mandant mandant) {



		Assert.isFalse(ziel.getLagerort() == null || ziel.getBglPosition() == null,

				BackendError.CANNOT_CREATE_TRANSPORT_DESTINATION_MISSING);



		T transport = ObjectHelper.newInstance(transportClass);

		String transpNummer = nkService.getNextValue("Transport_Number");

		transport.setNummer(transpNummer);

		transport.setStatus(TransportStatus.WAITING);

		transport.setMandant(mandant);

		transport.setLeTyp(le.getLetyp().getName());

		transport.setStandort(le.getStandort());

		transport.setZiel(ziel.getLagerort());

		transport.setBglZielPosition(ziel.getBglPosition());

		transport.setBglStandortPosition(le.getCurLagerortPos());

		transport.setWickelProgramm(0);

		transport.setIstLeer(le.getAnzBestand() == 0);

		transport.setAngelegt(localDateTimeProvider.getLocalDateTime());

		transport.setGeaendert(transport.getAngelegt());

		transport.setLeNummer(le.getNummer());

		transport.setLe(le);

		transport = transpRepository.save(transport);

		log.info("Created {}", transport);



		le.setBglPosition(ziel.getBglPosition());

		le.setStatus(LeStatus.IN_TRANSPORT);

		leService.update(le, EventType.CHANGE_STATUS, null);

		return transport;

	}



	@Override

	public T create(Class<T> transpClass, Long leId, Long targetBinId, Long sourceBinId, Integer targetPosition,

			Long mandantId, Integer priority, Long previousTranspId, Long nutzerId) {



		Assert.isTrue(targetPosition != null && priority != null, BackendError.MISSING_ONE_OF_THE_INPUTS);



		Le le = leService.findById(leId);

		Assert.isTrue(le.getLetyp() != null, BackendError.LETYP_LEER, le.getNummer());

		Assert.isTrue(le.getNummer() != null, BackendError.MISSING_LE_NUMMER, le.getNummer());

		Assert.isTrue(le.getStandort().isGesperrt() == false, BackendError.LAGERORT_IS_BLOCKED,

				le.getStandort().getName(), le.getNummer());



		// List<T> transpList = findByLe(le);

		// Assert.isEmpty(transpList,

		// BackendError.TRANSPORT_FOR_LE_ALREADY_EXIST, le.getNummer());



		Lagerort targetBin = lagerortService.findById(targetBinId);

		Mandant mandant = null;

		if (mandantId != null) {

			mandant = mandantService.findById(mandantId);

		}

		T transp = ObjectHelper.newInstance(transpClass);

		String transpNummer = nkService.getNextValue("Transport_Number");

		transp.setNummer(transpNummer);

		transp.setStatus(TransportStatus.WAITING);

		transp.setStatusInfo("");

		transp.setLeTyp(le.getLetyp().getName());

		if (sourceBinId != null) {

			Lagerort sourceBin = lagerortService.findById(sourceBinId);

			transp.setStandort(sourceBin);

		} else if (le.getStandort() != null) {

			transp.setStandort(le.getStandort());

		} else {

			transp.setStandort(le.getHerqunft());

		}

		transp.setZiel(targetBin);

		transp.setBglZielPosition(targetPosition);

		transp.setWickelProgramm(0);

		transp.setIstLeer(false);

		transp.setIstNio(false);

		transp.setHatKommAuf(false);

		transp.setAngelegt(localDateTimeProvider.getLocalDateTime());

		transp.setMandant(mandant);

		transp.setBglStandortPosition(targetPosition);

		transp.setLeNummer(le.getNummer());

		transp.setPriority(priority);

		transp.setLe(le);

		if (previousTranspId != null) {

			T previousTransp = findById(previousTranspId);

			transp.setPreviousTransp(previousTransp);

		}

		transp = transpRepository.save(transp);

		log.info("TranspService create, created transport: {}, lagerort: {}, called from: {}", transp, targetBin,

				Thread.currentThread().getStackTrace()[2].getClassName());

		le.setStatus(LeStatus.WAITING_TRAN);

		le.setBglPosition(targetPosition);

		leService.update(le, EventType.CHANGE_STATUS, nutzerId);

		log.info("TranspService create, transport: {} was attached to le:{}", transp.getNummer(), le.getNummer());

		return transp;

	}

	@Override

	public void cancel(T transport) {

		if (TransportStatus.isWartet(transport)) {

			// Ist schon auf WARTET, entfernen aber den Warte-Grund und ein

			// reserviertes Fahrzeug

			if (!Strings.isNullOrEmpty(transport.getStatusInfo()) || !Strings.isNullOrEmpty(transport.getSpsEbb())) {

				transport.setStatusInfo(null);

				transport.setSpsEbb(null);

				update(transport, EventType.CHANGE_STATUS);

			}

			return;

		}



		String sps = Strings.nullToEmpty(transport.getSps());

		if (sps.startsWith("RBG")) {

			// Info-Spruch analog zum Start/Finish in den Logger

			StringBuilder info = new StringBuilder();

			info.append(sps).append(' ');

			info.append(transport.getSpsInfo()).append("-CANCEL: ");

			info.append(transport.getNummer());

			info.append(" von ").append(Strings.padEnd(transport.getStandort().getName(), 14, ' '));

			info.append(" nach ").append(Strings.padEnd(transport.getZiel().getName(), 14, ' '));



		}



		clearSps(transport);

	}


	@Override

	public void start(T transport) {

		transport.setStatus(TransportStatus.STARTED);

		transport.setAktiviert(localDateTimeProvider.getLocalDateTime());

		update(transport, EventType.CHANGE_STATUS);

		Le le = transport.getLe();

		le.setStatus(LeStatus.IN_TRANSPORT);

		leService.update(le, EventType.CHANGE_STATUS, null);

		log.info("Transport was started: {}", transport.getNummer());

	}



	@Override

	public void start(T transport, Long nutzerId) {

		transport.setStatus(TransportStatus.STARTED);

		transport.setAktiviert(localDateTimeProvider.getLocalDateTime());

		update(transport, EventType.CHANGE_STATUS);

		Le le = transport.getLe();

		le.setStatus(LeStatus.IN_TRANSPORT);

		leService.update(le, EventType.CHANGE_STATUS, null);

		log.info("Transport was started: {}", transport.getNummer());

	}


	@Override

	public void delete(T transp, Le le) {

		leService.update(le, null, null);

		delete(transp);

		log.debug("trasport with nummer:{} for  leNummer:{} deleted", transp.getNummer(), le.getNummer());

	}



	@Override

	public void finish(Long transpId) {

		T transp = findById(transpId);

		transp.setStatus(TransportStatus.FINISHED);

		transp.setGeaendert(localDateTimeProvider.getLocalDateTime());

		update(transp, EventType.CHANGE_STATUS);

		Le le = transp.getLe();

		leService.dropLeAtBin(le, transp.getZiel(), transp.getBglZielPosition(), null);

		Assert.isTrue(transp.getZiel() != null, BackendError.LAGERORT_LEER);

		Assert.isTrue(transp.getZiel().getLzone() != null, BackendError.LZONE_ID_LEER);

		Assert.isTrue(transp.getZiel().getLzone().getLber() != null, BackendError.LBER_ID_LEER);

		Long targetAreaId = transp.getZiel().getLzone().getLber().getId();

		pickingSrcLeServiceImpl.setPickingSourceLeStatus(null, null, le.getId(), targetAreaId);

		receiptLeServiceImpl.setReceiptLeStatus(le.getId(), LeStatus.ALOCATED);

		T nextTransp = findByPreviousTransp(transp);

		if (nextTransp != null) {

			nextTransp.setPreviousTransp(null);

			update(nextTransp, null);

			start(nextTransp);

		}

		delete(transp);



		FinishLeEvent finishLeEvent = new FinishLeEvent(this, le.getId(), "finish");

		publishFinishLeEvent(finishLeEvent);

		SendAllocationEvent sendAllocationEvent = new SendAllocationEvent(this, le.getId());

		applicationEventPublisher.publishEvent(sendAllocationEvent);



	}




	@Override

	public void pickTransport(Long transpId, Long transpAidId, String termName) {

		T transp = findById(transpId);

		Le transpAid = leService.findById(transpAidId);

		transp.setStatus(TransportStatus.PICKED);

		transp.setGeaendert(localDateTimeProvider.getLocalDateTime());

		transp.setTranspAidNummer(transpAid.getNummer());

		if (termName != null) {

			transp.setTranspAidNummer(termName);

		}

		transp = update(transp, EventType.CHANGE_STATUS);

		Le le = transp.getLe();

		leService.pickLeFromBin(le, transpAid, null);

		Long targetLberId = transp.getZiel().getLzone().getLber().getId();

		pickingSrcLeServiceImpl.setPickingSourceLeStatus(null, PickingSrcLeStatus.IN_TRANSPORT, le.getId(),

				targetLberId);

	}



	@Override

	public List<T> findByStandort(Lagerort standort) {

		return transpRepository.findByStandort(standort);

	}



	@Override

	public List<T> findByZiel(Lagerort ziel) {

		return transpRepository.findByZiel(ziel);

	}



	@Override

	public List<TransportOrderDTO> findTransportOrders(Long leId) {

		String lberName = getConfValue("TRANSPORT", "EXPEDITION_GATE");

		List<TransportOrderDTO> transpOrderDtos = transpRepository.findTransportOrders(leId, lberName);

		return transpOrderDtos;

	}



	@Override

	public TranspLoadingCountDTO getTransportLoadingCount(Long leId) {

		String lberName = getConfValue("TRANSPORT", "EXPEDITION_GATE");

		Integer transportCount = transpRepository.findAllStartedTransportsForTransportAid(leId,

				Arrays.asList(TransportStatus.STARTED), lberName);

		Integer loadingCount = transpRepository.findAllStartedTransportsInShipmentOrders(leId,

				Arrays.asList(TransportStatus.STARTED), lberName);

		TranspLoadingCountDTO transpLoadingCountDTO = new TranspLoadingCountDTO();

		transpLoadingCountDTO.setTransportCount(transportCount);

		transpLoadingCountDTO.setLoadingCount(loadingCount);

		return transpLoadingCountDTO;

	}




	@Override

	public T findByNummer(String trNummer) {

		return transpRepository.findByNummer(trNummer);

	}



	@Override

	public List<StartedTransportsDto> findStartedTransports(String intfTypeName) {

		List<StartedTransportsDto> startedTransportsList = transpRepository.findStartedTransports(intfTypeName,

				TransportStatus.STARTED);



		for (StartedTransportsDto startedTransport : startedTransportsList) {

			T transp = findById(startedTransport.getId());

			transp.setStatus(TransportStatus.SENT);

			update(transp, EventType.CHANGE_STATUS);

		}

		return startedTransportsList;

	}



	@Override

	public List<T> findByLeNummer(String leNummer) {

		return transpRepository.findByLeNummer(leNummer);

	}



	@Override

	public StartedTransportsDto findTransportForTranspResponse(Long id) {

		List<StartedTransportsDto> traspList = transpRepository.findTransportForTranspResponse(id);

		if (!traspList.isEmpty()) {

			return traspList.get(0);

		}



		return null;

	}



	private String getConfValue(String section, String name) {

		Configuration conf = confService.findBySectionAndName(section, name);

		Assert.isTrue(conf != null, BackendError.NO_SUCH_CONFIGURATION);

		String shipmentTransportAid = conf.getValue();

		return shipmentTransportAid;

	}



	@Override

	public T findByPreviousTransp(T previousTransp) {

		return transpRepository.findByPreviousTransp(previousTransp);

	}



	@Override

	public T createMultiStepTransp(Class<T> transpClass, Long leId, Long sourceBinId, Long targetBinId,

			Integer targetPosition, Integer priority) {

		Long records = lzoneTranspRouteService.countLzoneTranspRoute();

		T firstTransp = null;

		Le le = leService.findById(leId);

		Long mandantId = le.getMandant() == null ? null : le.getMandant().getId();

		if (records == 0) {

			firstTransp = create(transpClass, leId, targetBinId, sourceBinId, targetPosition, mandantId, priority, null,

					null);

			return firstTransp;

		}

		List<Best> bests = bestService.findByLe(le);

		Assert.isTrue(!bests.isEmpty(), BackendError.BEST_NOT_FOUND);

		Best best = bests.get(0);

		Assert.isTrue(best.getArt() != null, BackendError.ARTIKEL_ID_LEER);

		Assert.isTrue(best.getArtUom() != null, BackendError.ART_UNIT_OF_MEASURE_ID_MISSING);

		ArtUnitOfMeasure auom = auomSrvice.findById(best.getArtUom().getId());

		Assert.isTrue(le.getLetyp() != null, BackendError.LETYP_NOT_FOUND);

		Letyp letyp = letypService.findById(le.getLetyp().getId());

		Lagerort sourceBin = lagerortService.findById(sourceBinId);

		Lzone sourceZone = sourceBin.getLzone();

		Assert.isTrue(sourceZone != null, BackendError.LZONE_ID_LEER);

		Lagerort targetBin = lagerortService.findById(targetBinId);

		Lzone targetZone = targetBin.getLzone();

		Assert.isTrue(targetZone != null, BackendError.LZONE_ID_LEER);

		List<LzoneTranspRoute> lzoneTrRoutes = lzoneTranspRouteService

				.findBySourceZoneAndTargetZoneAndDirectRoute(sourceZone, targetZone, true);

		if (!lzoneTrRoutes.isEmpty()) {

			firstTransp = create(transpClass, leId, targetBinId, sourceBinId, targetPosition, mandantId, priority, null,

					null);

			return firstTransp;

		} else {

			

			List<LzoneTranspRoute> allRoutes = lzoneTranspRouteService.findAll();

			List<Lzone> lzones = new ArrayList<Lzone>();

			Map<Long, Lzone> vertexes = new HashMap<Long, Lzone>();

			List<Edge> edges = new ArrayList<Edge>();

			

			for(LzoneTranspRoute route: allRoutes) {

				

				if(!lzones.contains(route.getSourceZone())) {

					lzones.add(route.getSourceZone());

				}

				if(!lzones.contains(route.getTargetZone())) {

					lzones.add(route.getTargetZone());

				}

			}	

			

			for(Lzone lzone : lzones) {

				vertexes.put(lzone.getId(), lzone);

			}

			

			for(LzoneTranspRoute route: allRoutes) {

				edges.add(new Edge(route.getId(), vertexes.get(route.getSourceZone().getId()), vertexes.get(route.getTargetZone().getId()), 1));

			}

			

			Graph graph = new Graph(new ArrayList<Lzone>(vertexes.values()) , edges);

			DijkstraImpl dijkstra = new DijkstraImpl(graph);



			dijkstra.execute(vertexes.get(sourceZone.getId()));

			LinkedList<Lzone> vertexesInPath = dijkstra.getPath(vertexes.get(targetZone.getId()));

			Long currentBinId = sourceBinId;

			for (int i = 0; i < vertexesInPath.size(); i++) {

				if(i<vertexesInPath.size() - 2){

					Long nextBinId = lagerortService.findPlaceInZone(auom.getLogisticType(), ProcessType.PUT, mandantId,

							letyp.getLeUnits(), Arrays.asList(new Long[] { vertexesInPath.get(i + 1).getId() }),

							le.getHoehe(),

							best.getArt().getId(), letyp.getId());

					if (nextBinId == -1) {

						log.info(

								"createMultiStepTransp: Not found free bin LogisticType: {}, zone {}, leTyp: {}, Le{}",

								auom.getLogisticType(), vertexesInPath.get(i), letyp, le.getNummer());

						return null;

					}

					create(transpClass, leId, nextBinId, currentBinId, 1,

							mandantId, priority, null, null);

					currentBinId = nextBinId;

				}

				else if(i == vertexesInPath.size() - 2){

					create(transpClass, leId, targetBinId, currentBinId, 1,

							mandantId, priority, null, null);

				}				

			}			

		}

		return firstTransp;

	}




	@Override

	public List<T> findByLe(Le le) {

		return transpRepository.findByLe(le);

	}



	@Override

	public TranspLoadingCountDTO getTransportLoadingCountForAll() {

		String lberName = getConfValue("TRANSPORT", "EXPEDITION_GATE");

		Integer transportCount = transpRepository.findAllStartedTransportsForAll(Arrays.asList(TransportStatus.STARTED),

				lberName);

		Integer loadingCount = transpRepository

				.findAllStartedTransportsInShipmentOrdersForAll(Arrays.asList(TransportStatus.STARTED), lberName);

		TranspLoadingCountDTO transpLoadingCountDTO = new TranspLoadingCountDTO();

		transpLoadingCountDTO.setTransportCount(transportCount);

		transpLoadingCountDTO.setLoadingCount(loadingCount);

		return transpLoadingCountDTO;

	}





}