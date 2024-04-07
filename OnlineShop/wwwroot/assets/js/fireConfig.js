import { initializeApp } from "https://www.gstatic.com/firebasejs/10.10.0/firebase-app.js";
import { getDatabase, ref, set, push, child, get, onValue, query, orderByChild, equalTo, startAt, endAt } from "https://www.gstatic.com/firebasejs/10.10.0/firebase-database.js";
                                            // TODO: Add SDKs for Firebase products that you want to use
                                            // https://firebase.google.com/docs/web/setup#available-libraries

                                            // Your web app's Firebase configuration
                                            // For Firebase JS SDK v7.20.0 and later, measurementId is optional


                                            // Initialize Firebase
export const firebaseConfig = {
    apiKey: "AIzaSyB4ks5xp-2GxHnXEnJgnqDxWwk39EGKavI",
    authDomain: "onlineshop-931f7.firebaseapp.com",
    databaseURL: "https://onlineshop-931f7-default-rtdb.asia-southeast1.firebasedatabase.app",
    projectId: "onlineshop-931f7",
    storageBucket: "onlineshop-931f7.appspot.com",
    messagingSenderId: "274249129077",
    appId: "1:274249129077:web:a9612dd7fa4d010a572c3c",
    measurementId: "G-F8TSGWE9VQ"
};

const app = initializeApp(firebaseConfig);
const db = getDatabase(app);
function writeUserData() {

    set(ref(db, 'chats/' + "chat_1"), {
        members: [1, 2],
    });
    set(ref(db, 'chats/chat_1/' + 'mess1'), {
        content: 1,
        sender: "loi"
    })

    const dbRef = ref(getDatabase(app));
    get(child(dbRef, `users`)).then((snapshot) => {
        if (snapshot.exists()) {
            snapshot.forEach((childSnapshot) => {
                //console.log(childSnapshot.key);
                //console.log(childSnapshot.val());
            });
        } else {
            console.log("No data available");
        }
    }).catch((error) => {
        console.error(error);
    });

    onValue(ref(getDatabase(app), '/users/1'), (snapshot) => {
        snapshot.forEach((childSnapshot) => {
            //console.log(childSnapshot.key);
            //console.log(childSnapshot.val());
        });
    }, {
        onlyOnce: true
    });

    //


    const chatsRef = ref(db, 'chats');

    onValue(chatsRef, (snapshot) => {
        snapshot.forEach((childSnapshot) => {
            const chatId = childSnapshot.key;
            const chatData = childSnapshot.val();

            // Kiểm tra nếu thành viên chứa user_id_1
            if (chatId.includes('1')) {
                console.log('Chat ID:', chatId);
                console.log('Chat Data:', chatData);
            }
        });
    }, {
        onlyOnce: true
    });
}
//writeUserData();
export function writeMessToChat(userId, sellerId, message) {
    const chatId = `chat_sellerId_${sellerId}_userId_${userId}`
    get(child(dbRef, `users/${chatId}`)).then((snapshot) => {
        snapshot.forEach((childSnapshot) => {
            var currentTime = new Date();
            var hours = currentTime.getHours().toString().padStart(2, '0');
            var minutes = currentTime.getMinutes().toString().padStart(2, '0');
            var timeString = hours + ":" + minutes;
            set(ref(db, `users/${chatId}/messages`), {
                senderId: userId,
                content: message,
                timeSend: timeString
            })
        })
    }).catch((error) => {
        console.error(error);
    });
}
