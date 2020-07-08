package bg.diplNS;

import java.io.IOException;
import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.concurrent.atomic.AtomicLong;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestMethod;
import org.springframework.web.bind.annotation.RequestParam;

import com.google.gson.Gson;
import com.google.gson.GsonBuilder;

import bg.diplNS.dto.TranspLoadingCountDTO;
import bg.diplNS.dto.TransportAidDTO;
import bg.diplNS.dto.TransportOrderDTO;
import bg.diplNS.enums.ResultStrings;
import bg.diplNS.error.BackendError;
import bg.diplNS.model.tables.Transp;
import bg.diplNS.remote.exc.OperatingException;
import bg.diplNS.remote.helper.Assert;
import bg.diplNS.service.TranspService;
import lombok.extern.slf4j.Slf4j;
import sun.misc.BASE64Decoder;

@Slf4j
public class ServiceController {


	@Autowired
	TranspService<Transp> transpService;


	@RequestMapping("/loadTransportAids")
	public String loadTransportAids(@RequestHeader(value = "Authorization", defaultValue = "NO_AUTH") String authString,
			@RequestHeader(value = "U", defaultValue = "") String user,
			@RequestHeader(value = "P", defaultValue = "") String pass) {

		if (!isUserAuthenticated(authString, user, pass)) {
			log.info("POST Response: " + this.deviceId + ":writeLastStatus " + AUTHERROR);
			return AUTHERROR;
		}
		List<TransportAidDTO> aids = leProcessing.loadTransportAids();
		String trAids = putIntoJson(aids);
		return trAids;
	}

	
	@RequestMapping("/loadTransportOrders")
	public String loadTransportOrders(@RequestParam Long leId,
			@RequestHeader(value = "Authorization", defaultValue = "NO_AUTH") String authString,
			@RequestHeader(value = "U", defaultValue = "") String user,
			@RequestHeader(value = "P", defaultValue = "") String pass) {

		if (!isUserAuthenticated(authString, user, pass)) {
			log.info("POST Response: " + this.deviceId + ":writeLastStatus " + AUTHERROR);
			return AUTHERROR;
		}

		List<TransportOrderDTO> orders = transpService.findTransportOrders(leId);
		String trOrders = putIntoJson(orders);
		return trOrders;
	}

	
	@RequestMapping("/pickTransport")
	public String pickTransport(@RequestParam Long trId, @RequestParam Long trAidId,
			@RequestHeader(value = "Authorization", defaultValue = "NO_AUTH") String authString,
			@RequestHeader(value = "U", defaultValue = "") String user,
			@RequestHeader(value = "P", defaultValue = "") String pass) {

		if (!isUserAuthenticated(authString, user, pass)) {
			log.info("POST Response: " + this.deviceId + ":writeLastStatus " + AUTHERROR);
			return AUTHERROR;
		}
		return executeWithStatus(() -> {
			transpService.pickTransport(trId, trAidId, null);
		});

	}

	
	@RequestMapping("/finishTransport")
	public String finishTransport(@RequestParam Long trId,
			@RequestHeader(value = "Authorization", defaultValue = "NO_AUTH") String authString,
			@RequestHeader(value = "U", defaultValue = "") String user,
			@RequestHeader(value = "P", defaultValue = "") String pass) {

		if (!isUserAuthenticated(authString, user, pass)) {
			log.info("POST Response: " + this.deviceId + ":writeLastStatus " + AUTHERROR);
			return AUTHERROR;
		}

		return executeWithStatus(() -> {
			Transp tr = transpService.findById(trId);
			Le le = leService.findByNummer(tr.getTranspAidNummer());
			leService.updateLePosition(le, tr.getZiel(), wmsAuthentication.get(user, pass).getNutzerId());
			transpService.finish(trId);
		});
	}

	
	@RequestMapping("/getTransportLoadingCount")
	public String getTransportLoadingCount(@RequestParam Long leId,
			@RequestHeader(value = "Authorization", defaultValue = "NO_AUTH") String authString,
			@RequestHeader(value = "U", defaultValue = "") String user,
			@RequestHeader(value = "P", defaultValue = "") String pass) {

		if (!isUserAuthenticated(authString, user, pass)) {
			log.info("POST Response: " + this.deviceId + ":writeLastStatus " + AUTHERROR);
			return AUTHERROR;
		}

		TranspLoadingCountDTO transpLoadingCountDTO = transpService.getTransportLoadingCount(leId);
		return putIntoJson(transpLoadingCountDTO);
	}
}
