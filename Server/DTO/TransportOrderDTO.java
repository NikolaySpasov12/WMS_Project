package bg.diplNS.dto;



import java.io.Serializable;



import bg.diplNS.model.tables.enums.TransportStatus;

import lombok.AllArgsConstructor;

import lombok.Data;

import lombok.NoArgsConstructor;


@Data

@AllArgsConstructor

@NoArgsConstructor

public class TransportOrderDTO implements Serializable {


	private static final long serialVersionUID = 1L;

	private Long transpId;

	private Long leId;

	private String leNummer;

	private String Letyp;

	private String sourceBinName;

	private String targetBinName;

	private int priority;

	private TransportStatus transpStatus;

	private int levelDigits;

}