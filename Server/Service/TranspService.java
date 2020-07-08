package bg.diplNS.service;



import java.util.List;



import bg.diplNS.dto.SeparetedLeDTO;

import bg.diplNS.dto.ShipmentTransportOrderDTO;

import bg.diplNS.dto.StartedTransportsDto;

import bg.diplNS.dto.TranspLoadingCountDTO;

import bg.diplNS.dto.TransportOrderDTO;

import bg.diplNS.events.FinishLeEvent;

import bg.diplNS.helper.LagerortWithPosition;

import bg.diplNS.model.tables.Lagerort;

import bg.diplNS.model.tables.Le;

import bg.diplNS.model.tables.Letyp;

import bg.diplNS.model.tables.Mandant;

import bg.diplNS.model.tables.Transp;

import bg.diplNS.model.tables.enums.EventType;




public interface TranspService<T extends Transp> {



	public List<T> findByZiel(long zielId);



	public T create(T entity);



	public void delete(T entity);



	public boolean standort(T transport, LagerortWithPosition standort, boolean cancelTransport);



	public void start(T transport, Lagerort zwischenZiel);



	public void cancel(T transport);



	public T findById(Long id);



	public List<T> findByStandort(Lagerort standort);



	public List<T> findByZiel(Lagerort ziel);



	public List<T> findByZwischenZiel(Lagerort ziel);



	public T create(Class<T> transportClass, Le le, LagerortWithPosition ziel, Mandant mandant);



	public void delete(T transp, Le le);



	boolean updateIfNeeded(T transp, LagerortWithPosition ziel);



	public boolean existsByStandort(Lagerort standort);



	public boolean existsByZwischenZiel(Lagerort zwischenZiel);



	public boolean existsByZiel(Lagerort ziel);



	public T create(Class<T> transpClass, Long leId, Long targetBinId, Long sourceBinId, Integer targetPosition,

			Long mandantId, Integer priority, Long previousTranspId, Long nutzerId);



	public void start(T transport);



	public void finish(Long transpId);



	public List<TransportOrderDTO> findTransportOrders(Long leId);

	public TranspLoadingCountDTO getTransportLoadingCount(Long leId);

	public T findByNummer(String trNummer);



	public List<StartedTransportsDto> findStartedTransports(String intfTypeName);



	public List<T> findByLeNummer(String leNummer);



	public StartedTransportsDto findTransportForTranspResponse(Long id);



	T update(T entity, EventType eventType);



	void publishFinishLeEvent(FinishLeEvent finishLeEvent);



	void createTransportToPickingArea(Class<T> transportClass);



	void createReturnTransportFromPickingArea(Class<T> transportClass);



	public T findByPreviousTransp(T previousTransp);



	T createMultiStepTransp(Class<T> transpClass, Long leId, Long sourceBinId, Long targetBinId, Integer targetPosition,

			Integer priority);



	public void createTransportToExpeditionGate(Class<T> transpClass, int priority);



	Integer countTransportsForLe(Long leId);



	Integer countPickedTransportsForLe(Long leId);



	public List<T> findByLe(Le le);



	public T createAllocationTransportForLe(Class<T> transportClass, Le le, Letyp letyp, Long artId,

			String bestellNummer, String sourceBinName);

	void createTransportToEinalgerungsPalletsWithNoTransport(Class<T> transportClass);



	void pickTransport(Long transpId, Long transpAidId, String termName);



	public TranspLoadingCountDTO getTransportLoadingCountForAll();



	SeparetedLeDTO createTransportForSeparatedLe(Le le, Class<T> transpClass);



	void start(T transport, Long nutzerId);



	String transpBinFull(String lagerortName, String storedLeNummer, String ongoingLeNummer, Long nutzerId);



	void deleteTranspBinEmpty(String lagerortName, String LeNummer);

}