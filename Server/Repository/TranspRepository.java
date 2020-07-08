
package bg.diplNS.repository;



import java.util.List;



import org.springframework.data.jpa.repository.JpaRepository;

import org.springframework.data.jpa.repository.Query;

import org.springframework.data.repository.query.Param;

import org.springframework.stereotype.Repository;



import bg.diplNS.dto.ShipmentTransportOrderDTO;

import bg.diplNS.dto.StartedTransportsDto;

import bg.diplNS.dto.TransportOrderDTO;

import bg.diplNS.model.tables.Lagerort;

import bg.diplNS.model.tables.Le;

import bg.diplNS.model.tables.Transp;

import bg.diplNS.model.tables.enums.TransportStatus;



@Repository

public interface TranspRepository<T extends Transp> extends JpaRepository<T, Long> {



	@Query(value = "select * from " + "TRANSP" + " where " + "ZIEL_ID" + " = :zielId"

			+ " order by GEAENDERT", nativeQuery = true)

	public List<T> findByZiel(@Param("zielId") long zielId);



	public List<T> findByStandort(Lagerort entity);



	public List<T> findByZiel(Lagerort ziel);



	public List<T> findByZwischenZiel(Lagerort zwischenZiel);



	public boolean existsByStandort(Lagerort standort);



	public boolean existsByZwischenZiel(Lagerort zwischenZiel);



	public boolean existsByZiel(Lagerort ziel);




	@Query(value = "SELECT  new bg.diplNS.dto.TransportOrderDTO (tr.id,tr.le.id"

			+ "		,tr.le.nummer, tr.le.letyp.name,tr.standort.name,tr.ziel.name"

			+ "		 ,tr.priority,tr.status,tr.ziel.lzone.lber.levelDigits )"

			+ "		 FROM Transp tr, LzoneDistance ld, Le le"

			+ "		WHERE (le.id=:leId) AND"

			+ " 	((ld.sourceZone.id = le.standort.lzone.id AND ld.targetZone.id = tr.standort.lzone.id) AND \r\n"

			+ "		((tr.status='PICKED' AND tr.transpAidNummer=(SELECT le.nummer FROM Le le where le.id=:leId) )\r\n"

			+ "		 OR (tr.status='STARTED' AND tr.transpAidNummer IS NULL))\r\n"

			+ "		 AND tr.standort.lzone.lber IN (select trAidLberMapping.lber FROM\r\n"

			+ "		 TranspAidLberMapping trAidLberMapping WHERE trAidLberMapping.le.id=:leId)\r\n"

			+ "		 AND tr.ziel.lzone.lber IN (select trAidLberMapping.lber FROM\r\n"

			+ "		 TranspAidLberMapping trAidLberMapping WHERE trAidLberMapping.le.id=:leId)\r\n"

			+ "		 AND tr.ziel.lzone.lber.name!=:lberName)\r\n"

			+ "		 order by  (CASE\r\n"

			+ "		 WHEN ld.distance = 0 AND tr.standort.fieldNumber < tr.ziel.fieldNumber THEN (tr.ziel.fieldNumber - tr.standort.fieldNumber)\r\n"

			+ "		 WHEN ld.distance = 0 AND tr.standort.fieldNumber > tr.ziel.fieldNumber THEN (tr.standort.fieldNumber - tr.ziel.fieldNumber)\r\n"

			+ "		 ELSE (ld.distance + tr.standort.fieldNumber + tr.ziel.fieldNumber)\r\n"

			+ "		 END) asc, tr.status asc, priority desc, tr.standort.binOrderAsc ASC ")

	public List<TransportOrderDTO> findTransportOrders(@Param("leId") Long leId, @Param("lberName") String lberName);





	public T findByNummer(String trNummer);





	@Query("SELECT tr FROM Transp tr WHERE tr.leNummer=:leNummer")

	public List<T> findByLeNummer(@Param("leNummer") String leNummer);


	@Query(value = "select CAST(CASE WHEN COUNT(le) = 1 THEN 1 ELSE 0 END AS boolean) from Le le WHERE le.id = :leId AND le.id IN (SELECT le.id FROM Le le WHERE le.nummer IN(:shipmentTrAid))")

	public boolean isLoadingTranspAid(@Param("leId") Long leId, @Param("shipmentTrAid") List<String> shipmentTrAid);



	public T findByPreviousTransp(T previousTransp);



	@Query(value = "SELECT count(tr) from Transp tr WHERE tr.le.id = :leId")

	public Integer countTransportsForLe(@Param("leId") Long leId);



	@Query(value = "SELECT count(tr) from Transp tr WHERE tr.le.id = :leId AND tr.status = :status")

	public Integer countPickedTransportsForLeByStatus(@Param("leId") Long leId,

			@Param("status") TransportStatus status);



	public List<T> findByLe(Le le);



	@Query(value = "SELECT count(tr) FROM Transp tr WHERE tr.status IN (:trStatuses)"

			+ " AND tr.standort.lzone.lber IN (SELECT trAidLberMapping.lber FROM TranspAidLberMapping trAidLberMapping"

			+ " WHERE trAidLberMapping.le.id NOT IN (select term.id from Terminal term where term.leId is not null ) )" 			

			+ " AND tr.ziel.lzone.lber.name!=:lberName")

	public Integer findAllStartedTransportsForAll(@Param("trStatuses") List<TransportStatus> trStatuses,

			@Param("lberName") String lberName);



	@Query(value = "SELECT count(tr) FROM Transp tr WHERE tr.status IN (:trStatuses)"

			+ " AND tr.ziel.lzone.lber.name=:lberName")

	public Integer findAllStartedTransportsInShipmentOrdersForAll(@Param("trStatuses") List<TransportStatus> trStatuses,

			@Param("lberName") String lberName);

}